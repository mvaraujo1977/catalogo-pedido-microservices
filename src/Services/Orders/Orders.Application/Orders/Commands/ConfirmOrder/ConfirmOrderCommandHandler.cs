using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Commands.ConfirmOrder;

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _repository;

    public ConfirmOrderCommandHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<OrderDto>> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result<OrderDto>.Failure("Pedido não encontrado.");

        try
        {
            order.Confirm();
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderDto>.Failure(ex.Message);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result<OrderDto>.Success(order.ToDto());
    }
}
