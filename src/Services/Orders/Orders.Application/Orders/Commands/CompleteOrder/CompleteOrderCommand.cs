using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Commands.CompleteOrder;

public record CompleteOrderCommand(Guid Id) : IRequest<Result<OrderDto>>;
