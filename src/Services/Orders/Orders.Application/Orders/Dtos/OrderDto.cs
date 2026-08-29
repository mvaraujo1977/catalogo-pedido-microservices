namespace Orders.Application.Orders.Dtos;

public record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);
