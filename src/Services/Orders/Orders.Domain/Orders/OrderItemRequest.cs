namespace Orders.Domain.Orders;

/// <summary>
/// Contrato de entrada para criação de um item de pedido. ProductName e
/// UnitPrice já vêm resolvidos (via chamada ao Catalog ou informados
/// pelo chamador) antes de chegar no Domain — o Domain não sabe de onde
/// vieram, só valida e usa.
/// </summary>
public record OrderItemRequest(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
