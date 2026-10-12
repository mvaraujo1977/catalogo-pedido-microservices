using BuildingBlocks.Messaging;
using Catalog.Application.Common;
using Catalog.Application.IntegrationEvents;
using Catalog.Domain.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Products.Commands.ReserveStock;

public class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ReserveStockCommandHandler> _logger;

    public ReserveStockCommandHandler(
        IProductRepository repository,
        IEventPublisher eventPublisher,
        ILogger<ReserveStockCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        // Validar TODOS os itens antes de alterar qualquer um. Se
        // baixássemos item a item, um pedido de 3 itens cujo terceiro
        // está sem estoque deixaria os dois primeiros baixados num
        // pedido que será cancelado (reserva parcial).
        // Agrupar por ProductId antes de validar: um pedido pode listar
        // o mesmo produto duas vezes (3 + 3 contra estoque 5). Validando
        // item a item, cada um passaria contra o estoque original e o
        // segundo AdjustStock estouraria, derrubando a mensagem sem
        // publicar o evento de falha. Agrupado, a soma (6) é comparada
        // uma única vez contra o estoque.
        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        var resolved = new List<(Product Product, int Quantity)>();

        foreach (var item in requested)
        {
            var product = await _repository.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is null)
            {
                return await FailAsync(request.OrderId,
                    $"Produto {item.ProductId} não encontrado.", cancellationToken);
            }

            if (!product.Active)
            {
                return await FailAsync(request.OrderId,
                    $"Produto '{product.Name}' está inativo.", cancellationToken);
            }

            if (product.StockQuantity < item.Quantity)
            {
                return await FailAsync(request.OrderId,
                    $"Estoque insuficiente para '{product.Name}': disponível {product.StockQuantity}, solicitado {item.Quantity}.",
                    cancellationToken);
            }

            resolved.Add((product, item.Quantity));
        }

        foreach (var (product, quantity) in resolved)
        {
            product.AdjustStock(-quantity);
        }

        // Um único SaveChanges: todas as baixas de estoque numa
        // transação só.
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Estoque reservado para o pedido {OrderId}", request.OrderId);

        await _eventPublisher.PublishAsync(
            new StockReservedEvent(request.OrderId, DateTimeOffset.UtcNow),
            StockRoutingKeys.Reserved,
            cancellationToken);

        return Result.Success();
    }

    private async Task<Result> FailAsync(Guid orderId, string reason, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Reserva de estoque falhou para o pedido {OrderId}: {Reason}", orderId, reason);

        await _eventPublisher.PublishAsync(
            new StockReservationFailedEvent(orderId, reason, DateTimeOffset.UtcNow),
            StockRoutingKeys.ReservationFailed,
            cancellationToken);

        // Falha de estoque é um resultado de negócio esperado, não um
        // erro de processamento: retornamos Success para que o consumer
        // dê ACK na mensagem. O evento de falha já informou o Orders.
        return Result.Success();
    }
}
