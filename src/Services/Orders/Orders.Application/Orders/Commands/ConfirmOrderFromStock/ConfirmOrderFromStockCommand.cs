using Orders.Application.Common;
using MediatR;

namespace Orders.Application.Orders.Commands.ConfirmOrderFromStock;

/// <summary>
/// Confirmação iniciada pelo SISTEMA, em reação a stock.reserved — e não
/// por um usuário via HTTP. Por isso não existe checagem de propriedade
/// aqui: num BackgroundService não há HttpContext, e o ICurrentUserService
/// devolveria null, fazendo a checagem de dono rejeitar a operação.
/// O ConfirmOrderCommand (endpoint manual) continua existindo e mantém
/// sua checagem de dono.
/// </summary>
public record ConfirmOrderFromStockCommand(Guid OrderId) : IRequest<Result>;
