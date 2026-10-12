namespace Catalog.Application.IntegrationEvents;

public record StockReservedEvent(Guid OrderId, DateTimeOffset ReservedAt);

public record StockReservationFailedEvent(Guid OrderId, string Reason, DateTimeOffset FailedAt);

public static class StockRoutingKeys
{
    public const string Reserved = "stock.reserved";
    public const string ReservationFailed = "stock.reservation-failed";
}
