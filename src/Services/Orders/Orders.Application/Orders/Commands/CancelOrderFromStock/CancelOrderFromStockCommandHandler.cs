using Orders.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Orders.Application.Orders.Commands.CancelOrderFromStock;

public class CancelOrderFromStockCommandHandler : IRequestHandler<CancelOrderFromStockCommand, Result>
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<CancelOrderFromStockCommandHandler> _logger;

    public CancelOrderFromStockCommandHandler(
        IOrderRepository repository,
        ILogger<CancelOrderFromStockCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelOrderFromStockCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("stock.reservation-failed recebido para pedido inexistente {OrderId}", request.OrderId);
            return Result.Success();
        }

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogInformation(
                "Pedido {OrderId} não precisou ser cancelado: {Motivo}", request.OrderId, ex.Message);
            return Result.Success();
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Pedido {OrderId} cancelado por falha na reserva de estoque: {Reason}",
            request.OrderId, request.Reason);

        // Nenhum order.cancelled é publicado aqui de propósito: o
        // estoque nunca foi reservado para este pedido, então não há
        // nada para o Catalog devolver. Publicar levaria o Catalog a
        // CREDITAR estoque que nunca foi debitado.
        return Result.Success();
    }
}
