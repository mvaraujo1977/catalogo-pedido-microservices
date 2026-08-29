using Orders.Domain.Orders;

namespace Orders.Application.Orders.Dtos;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order) =>
        new(order.Id,
            order.UserId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal)).ToList());
}
