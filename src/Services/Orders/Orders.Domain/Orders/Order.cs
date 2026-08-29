using Orders.Domain.Common;

namespace Orders.Domain.Orders;

public class Order : Entity
{
    private readonly List<OrderItem> _items = new();

    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // uso exclusivo do EF Core

    private Order(Guid id, Guid userId) : base(id)
    {
        UserId = userId;
        Status = OrderStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Order Create(Guid userId, IReadOnlyCollection<OrderItemRequest> items)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("Um pedido precisa de pelo menos um item.", nameof(items));

        var order = new Order(Guid.NewGuid(), userId);

        foreach (var item in items)
        {
            order._items.Add(new OrderItem(order.Id, item.ProductId, item.ProductName, item.UnitPrice, item.Quantity));
        }

        order.TotalAmount = order._items.Sum(i => i.LineTotal);

        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Não é possível confirmar um pedido no status {Status}.");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException($"Não é possível cancelar um pedido no status {Status}.");

        Status = OrderStatus.Cancelled;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Não é possível concluir um pedido no status {Status}.");

        Status = OrderStatus.Completed;
    }
}
