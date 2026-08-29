using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Commands.CompleteOrder;

public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CompleteOrderCommandHandler(IOrderRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result<OrderDto>.Failure(OrderErrors.NotFoundOrNotOwned);

        var isOwner = _currentUser.UserId == order.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isOwner && !isAdmin)
            return Result<OrderDto>.Failure(OrderErrors.NotFoundOrNotOwned);

        try
        {
            order.Complete();
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderDto>.Failure(ex.Message);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result<OrderDto>.Success(order.ToDto());
    }
}
