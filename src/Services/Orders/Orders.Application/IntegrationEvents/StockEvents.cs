namespace Orders.Application.IntegrationEvents;

/// <summary>
/// Versão do Orders dos eventos publicados pelo Catalog. Declara apenas
/// o que o Orders precisa — o contrato real é o JSON.
/// </summary>
public record StockReservedEvent(Guid OrderId);

public record StockReservationFailedEvent(Guid OrderId, string Reason);

public static class StockRoutingKeys
{
    public const string Reserved = "stock.reserved";
    public const string ReservationFailed = "stock.reservation-failed";
}
