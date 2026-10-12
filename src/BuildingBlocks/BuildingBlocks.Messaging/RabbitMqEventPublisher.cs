using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace BuildingBlocks.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqConnection _connection;
    private readonly EventBusOptions _options;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RabbitMqEventPublisher(
        RabbitMqConnection connection,
        IOptions<EventBusOptions> options,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, JsonOptions));

        var properties = new BasicProperties
        {
            // Persistent grava a mensagem em disco. Fila durável com
            // mensagem não-persistente perde tudo no restart do broker:
            // precisa dos dois.
            Persistent = true,
            ContentType = "application/json",
            Type = typeof(TEvent).Name,
            // MessageId é a chave da idempotência na Fase 6: permite ao
            // consumidor reconhecer uma mensagem já processada.
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Evento {EventType} publicado na exchange {Exchange} com routing key {RoutingKey} (MessageId {MessageId})",
            typeof(TEvent).Name, _options.ExchangeName, routingKey, properties.MessageId);
    }
}
