using System.Text.Json;
using BuildingBlocks.Messaging;
using Catalog.Application.IntegrationEvents;
using Catalog.Application.Products.Commands.ReserveStock;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Consome eventos de pedido e traduz a mensagem em um comando da
/// Application. Nenhuma regra de negócio aqui: a reserva de estoque
/// vive no ReserveStockCommandHandler, testável sem RabbitMQ.
/// </summary>
public class OrderEventsConsumer : RabbitMqConsumerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderEventsConsumer> _logger;

    public OrderEventsConsumer(
        RabbitMqConnection connection,
        IOptions<EventBusOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderEventsConsumer> logger)
        : base(connection, options, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override string QueueName => "catalog.order-events";

    protected override string[] RoutingKeys => new[] { "order.created" };

    protected override async Task HandleMessageAsync(string messageType, string body, CancellationToken cancellationToken)
    {
        if (messageType != nameof(OrderCreatedEvent))
        {
            _logger.LogWarning("Mensagem {MessageType} ignorada na fila {Queue}", messageType, QueueName);
            return;
        }

        var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(body, JsonOptions)
            ?? throw new InvalidOperationException("Não foi possível desserializar OrderCreatedEvent.");

        // O BackgroundService é singleton, mas DbContext e handlers são
        // scoped: cada mensagem ganha seu próprio escopo, equivalente ao
        // que o ASP.NET faz por requisição HTTP.
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ReserveStockCommand(
            @event.OrderId,
            @event.Items.Select(i => new ReserveStockItem(i.ProductId, i.Quantity)).ToList());

        await mediator.Send(command, cancellationToken);
    }
}
