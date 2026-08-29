using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Commands.CreateOrder;

public record CreateOrderItemRequest(Guid ProductId, int Quantity);

public record CreateOrderCommand(Guid UserId, IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<Result<OrderDto>>;
