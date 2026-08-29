using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid Id) : IRequest<Result<OrderDto>>;
