using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using Orders.Application.Products;
using Orders.Domain.Orders;
using MediatR;

namespace Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICatalogServiceClient _catalogServiceClient;

    public CreateOrderCommandHandler(IOrderRepository orderRepository, ICatalogServiceClient catalogServiceClient)
    {
        _orderRepository = orderRepository;
        _catalogServiceClient = catalogServiceClient;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItemRequest>();

        foreach (var item in request.Items)
        {
            var product = await _catalogServiceClient.GetProductAsync(item.ProductId, cancellationToken);
            if (product is null)
                return Result<OrderDto>.Failure($"Produto {item.ProductId} não encontrado ou indisponível.");

            if (!product.Active)
                return Result<OrderDto>.Failure($"Produto '{product.Name}' não está mais disponível.");

            orderItems.Add(new OrderItemRequest(product.ProductId, product.Name, product.Price, item.Quantity));
        }

        var order = Order.Create(request.UserId, orderItems);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return Result<OrderDto>.Success(order.ToDto());
    }
}
