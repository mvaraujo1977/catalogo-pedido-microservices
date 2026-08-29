using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid Id) : IRequest<Result<OrderDto>>;
