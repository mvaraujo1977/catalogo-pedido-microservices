namespace Orders.Application.Orders;

/// <summary>
/// A mesma mensagem serve tanto para "pedido inexistente" quanto para
/// "pedido existe mas não é seu" — de propósito, para não vazar a
/// existência de pedidos de outros usuários (proteção IDOR/BOLA).
/// </summary>
public static class OrderErrors
{
    public const string NotFoundOrNotOwned = "Pedido não encontrado.";
}
