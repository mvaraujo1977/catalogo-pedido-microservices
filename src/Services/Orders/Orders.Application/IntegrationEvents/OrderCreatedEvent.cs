namespace Orders.Application.IntegrationEvents;

/// <summary>
/// Publicado quando um pedido é criado. O contrato real entre os
/// serviços é o JSON — o Catalog tem seu próprio record com apenas os
/// campos que usa, por isso adicionar um campo aqui não quebra lá.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderCreatedItem> Items);

public record OrderCreatedItem(Guid ProductId, string ProductName, int Quantity);

public static class OrderRoutingKeys
{
    public const string Created = "order.created";
    public const string Confirmed = "order.confirmed";
    public const string Cancelled = "order.cancelled";
}

public record OrderConfirmedEvent(Guid OrderId, Guid UserId, decimal TotalAmount);

public record OrderCancelledEvent(Guid OrderId, Guid UserId);
