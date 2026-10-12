using BuildingBlocks.Messaging;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Substitui o publisher real nos testes: registra as publicações em
/// memória, sem abrir conexão com o RabbitMQ.
/// </summary>
public class FakeEventPublisher : IEventPublisher
{
    private readonly List<(string RoutingKey, object Event)> _published = new();

    public IReadOnlyList<(string RoutingKey, object Event)> Published => _published;

    public Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        lock (_published)
        {
            _published.Add((routingKey, @event));
        }

        return Task.CompletedTask;
    }
}
