using Orders.Application.Common;
using Orders.Application.Orders.Dtos;
using MediatR;

namespace Orders.Application.Orders.Queries.GetOrders;

/// <summary>
/// UserId nulo lista pedidos de todos os usuários (uso administrativo).
/// A decisão de QUEM pode passar UserId nulo é responsabilidade da API
/// (Fase 4, próxima parte), não desta camada.
/// </summary>
public record GetOrdersQuery(Guid? UserId, int Page = 1, int PageSize = 20) : IRequest<Result<IReadOnlyList<OrderDto>>>;
