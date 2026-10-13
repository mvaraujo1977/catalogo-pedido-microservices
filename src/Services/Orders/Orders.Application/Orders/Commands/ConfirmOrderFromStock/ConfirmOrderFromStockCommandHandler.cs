using BuildingBlocks.Messaging;
using Orders.Application.Common;
using Orders.Application.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Orders.Application.Orders.Commands.ConfirmOrderFromStock;

public class ConfirmOrderFromStockCommandHandler : IRequestHandler<ConfirmOrderFromStockCommand, Result>
{
    private readonly IOrderRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ConfirmOrderFromStockCommandHandler> _logger;

    public ConfirmOrderFromStockCommandHandler(
        IOrderRepository repository,
        IEventPublisher eventPublisher,
        ILogger<ConfirmOrderFromStockCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmOrderFromStockCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("stock.reserved recebido para pedido inexistente {OrderId}", request.OrderId);
            return Result.Success();
        }

        try
        {
            order.Confirm();
        }
        catch (InvalidOperationException ex)
        {
            // Pedido já confirmado (pelo endpoint manual, ou por uma
            // entrega duplicada do broker). Não é erro de processamento:
            // o estado desejado já está lá, então damos ACK.
            _logger.LogInformation(
                "Pedido {OrderId} não precisou ser confirmado: {Motivo}", request.OrderId, ex.Message);
            return Result.Success();
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pedido {OrderId} confirmado após reserva de estoque", request.OrderId);

        await _eventPublisher.PublishAsync(
            new OrderConfirmedEvent(order.Id, order.UserId, order.TotalAmount),
            OrderRoutingKeys.Confirmed,
            cancellationToken);

        return Result.Success();
    }
}
