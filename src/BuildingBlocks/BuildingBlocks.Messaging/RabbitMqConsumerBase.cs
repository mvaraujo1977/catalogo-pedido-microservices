using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Base para consumidores de eventos de integração. Cada serviço herda
/// desta classe, informa o nome da sua fila e as routing keys que lhe
/// interessam, e implementa HandleMessageAsync.
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger _logger;

    protected readonly EventBusOptions Options;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    protected RabbitMqConsumerBase(
        RabbitMqConnection connection,
        IOptions<EventBusOptions> options,
        ILogger logger)
    {
        _connection = connection;
        Options = options.Value;
        _logger = logger;
    }

    /// <summary>Nome da fila deste serviço.</summary>
    protected abstract string QueueName { get; }

    /// <summary>Routing keys que esta fila quer receber (ex: "order.created").</summary>
    protected abstract string[] RoutingKeys { get; }

    /// <summary>
    /// Processa uma mensagem. Se lançar exceção, a mensagem recebe NACK.
    /// </summary>
    protected abstract Task HandleMessageAsync(string messageType, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Laço de reconexão com backoff exponencial limitado. Sem isto,
    /// uma exceção aqui derrubaria o host inteiro (no .NET 8 o
    /// comportamento padrão de BackgroundServiceExceptionBehavior é
    /// StopHost) — um broker indisponível tiraria do ar até os
    /// endpoints HTTP que não usam mensageria.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(5);
        var maxDelay = TimeSpan.FromSeconds(60);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);

                // Saiu sem exceção: conexão caiu de forma limpa.
                // Reinicia o backoff para reconectar rápido.
                delay = TimeSpan.FromSeconds(5);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown da aplicação: não é falha, apenas encerra.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Consumidor da fila {Queue} falhou. Nova tentativa em {Delay}s.",
                    QueueName, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var next = delay.TotalSeconds * 2;
                delay = TimeSpan.FromSeconds(Math.Min(next, maxDelay.TotalSeconds));
            }
        }

        _logger.LogInformation("Consumidor da fila {Queue} encerrado.", QueueName);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        await using var channel = await _connection.CreateChannelAsync(stoppingToken);

        // Declarações são idempotentes: quem subir primeiro cria, quem
        // vier depois apenas confirma. Por isso a ordem de startup dos
        // serviços não importa.
        await channel.ExchangeDeclareAsync(
            exchange: Options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        foreach (var routingKey in RoutingKeys)
        {
            await channel.QueueBindAsync(
                queue: QueueName,
                exchange: Options.ExchangeName,
                routingKey: routingKey,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Fila {Queue} ligada à exchange {Exchange} com routing key {RoutingKey}",
                QueueName, Options.ExchangeName, routingKey);
        }

        // Sem prefetch, o broker despeja a fila inteira no consumidor de
        // uma vez. Com 1, só entrega a próxima após o ACK da anterior.
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, eventArgs) => OnMessageReceivedAsync(channel, eventArgs, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Consumidor iniciado na fila {Queue}", QueueName);

        // Mantém o canal vivo. Se a conexão cair, IsOpen fica false e
        // saímos para que o laço externo reconecte.
        while (!stoppingToken.IsCancellationRequested && channel.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task OnMessageReceivedAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        var messageType = eventArgs.BasicProperties.Type ?? "desconhecido";
        var messageId = eventArgs.BasicProperties.MessageId ?? "sem-id";
        var body = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        try
        {
            await HandleMessageAsync(messageType, body, stoppingToken);

            // ACK manual: a mensagem só sai da fila depois de
            // processada com sucesso. Com autoAck: true ela sairia no
            // momento da entrega, e uma falha a perderia.
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Mensagem {MessageType} ({MessageId}) processada na fila {Queue}",
                messageType, messageId, QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao processar {MessageType} ({MessageId}) na fila {Queue}",
                messageType, messageId, QueueName);

            // requeue: false DESCARTA a mensagem. É proposital por
            // enquanto: com requeue: true, um erro determinístico
            // (JSON inválido, por exemplo) criaria loop infinito —
            // a mensagem voltaria, falharia, voltaria. A Fase 6
            // troca o descarte por uma dead letter queue.
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
        }
    }
}
