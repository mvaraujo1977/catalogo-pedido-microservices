using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    private readonly IOrderRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetOrdersQueryHandler(IOrderRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = _currentUser.IsInRole("Admin");
        var userIdFilter = isAdmin ? request.UserId : _currentUser.UserId;

        var orders = await _repository.GetPagedAsync(userIdFilter, request.Page, request.PageSize, cancellationToken);
        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }
}
