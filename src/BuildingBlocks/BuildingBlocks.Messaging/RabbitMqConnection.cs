using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Mantém UMA conexão TCP por processo (abrir conexão é caro). Os canais
/// (IChannel) é que são criados por operação, porque um canal NÃO é
/// thread-safe — dois publishes concorrentes no mesmo canal corrompem o
/// protocolo AMQP.
/// </summary>
public class RabbitMqConnection : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;

    public RabbitMqConnection(IOptions<EventBusOptions> options, ILogger<RabbitMqConnection> logger)
    {
        var config = options.Value;
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
            ClientProvidedName = config.ClientName,

            // Desabilitado de propósito: o RabbitMqConsumerBase já tem
            // laço de reconexão com backoff, e a recuperação automática
            // do cliente competiria com ele — o nosso laço abriria uma
            // conexão nova enquanto o cliente ainda tentava recuperar a
            // antiga, deixando conexões órfãs. Um único mecanismo, com
            // comportamento previsível. A recuperação automática também
            // não cobre o caso de o broker nunca ter respondido no
            // startup, que é justamente o que precisamos tratar.
            AutomaticRecoveryEnabled = false,
            TopologyRecoveryEnabled = false,
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            // Descarta uma conexão anterior fechada antes de abrir a
            // nova, para não acumular objetos de conexão mortos.
            if (_connection is not null)
            {
                try
                {
                    await _connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao descartar a conexão anterior do RabbitMQ.");
                }

                _connection = null;
            }

            _logger.LogInformation("Abrindo conexão com o RabbitMQ em {Host}:{Port}", _factory.HostName, _factory.Port);
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _lock.Dispose();
    }
}
