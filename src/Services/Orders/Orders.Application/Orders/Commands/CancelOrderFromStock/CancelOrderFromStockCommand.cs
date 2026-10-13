using Orders.Application.Common;
using MediatR;

namespace Orders.Application.Orders.Commands.CancelOrderFromStock;

/// <summary>
/// Cancelamento iniciado pelo sistema, em reação a
/// stock.reservation-failed. Mesma lógica do ConfirmOrderFromStock:
/// sem checagem de propriedade, porque não há usuário envolvido.
/// </summary>
public record CancelOrderFromStockCommand(Guid OrderId, string Reason) : IRequest<Result>;
