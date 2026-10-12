using Catalog.Application.Common;
using MediatR;

namespace Catalog.Application.Products.Commands.ReserveStock;

public record ReserveStockItem(Guid ProductId, int Quantity);

public record ReserveStockCommand(Guid OrderId, IReadOnlyList<ReserveStockItem> Items) : IRequest<Result>;
