using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(IOrderRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result<OrderDto>.Failure(OrderErrors.NotFoundOrNotOwned);

        var isOwner = _currentUser.UserId == order.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Result<OrderDto>.Failure(OrderErrors.NotFoundOrNotOwned);

        return Result<OrderDto>.Success(order.ToDto());
    }
}
