using System.Text.Json;
using BuildingBlocks.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Application.IntegrationEvents;
using Orders.Application.Orders.Commands.CancelOrderFromStock;
using Orders.Application.Orders.Commands.ConfirmOrderFromStock;

namespace Orders.Infrastructure.Messaging;

public class StockEventsConsumer : RabbitMqConsumerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockEventsConsumer> _logger;

    public StockEventsConsumer(
        RabbitMqConnection connection,
        IOptions<EventBusOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<StockEventsConsumer> logger)
        : base(connection, options, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override string QueueName => "orders.stock-events";

    // Uma fila ligada a DUAS routing keys: os dois resultados possíveis
    // da reserva chegam no mesmo consumidor, que decide pelo tipo.
    protected override string[] RoutingKeys => new[]
    {
        StockRoutingKeys.Reserved,
        StockRoutingKeys.ReservationFailed,
    };

    protected override async Task HandleMessageAsync(string messageType, string body, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        switch (messageType)
        {
            case nameof(StockReservedEvent):
            {
                var @event = JsonSerializer.Deserialize<StockReservedEvent>(body, JsonOptions)
                    ?? throw new InvalidOperationException("Não foi possível desserializar StockReservedEvent.");

                await mediator.Send(new ConfirmOrderFromStockCommand(@event.OrderId), cancellationToken);
                break;
            }

            case nameof(StockReservationFailedEvent):
            {
                var @event = JsonSerializer.Deserialize<StockReservationFailedEvent>(body, JsonOptions)
                    ?? throw new InvalidOperationException("Não foi possível desserializar StockReservationFailedEvent.");

                await mediator.Send(new CancelOrderFromStockCommand(@event.OrderId, @event.Reason), cancellationToken);
                break;
            }

            default:
                _logger.LogWarning("Mensagem {MessageType} ignorada na fila {Queue}", messageType, QueueName);
                break;
        }
    }
}
