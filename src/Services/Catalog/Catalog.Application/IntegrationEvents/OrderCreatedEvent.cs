namespace Catalog.Application.IntegrationEvents;

/// <summary>
/// Versão do Catalog do evento publicado pelo Orders. Declara apenas os
/// campos necessários para reservar estoque — TotalAmount, UserId e
/// CreatedAt são ignorados na desserialização.
/// </summary>
public record OrderCreatedEvent(Guid OrderId, IReadOnlyList<OrderCreatedItem> Items);

public record OrderCreatedItem(Guid ProductId, int Quantity);
