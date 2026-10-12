using BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using Orders.Application.Common;
using Orders.Application.IntegrationEvents;
using Orders.Application.Orders.Dtos;
using Orders.Application.Products;
using Orders.Domain.Orders;
using MediatR;

namespace Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICatalogServiceClient catalogServiceClient,
        IEventPublisher eventPublisher,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _catalogServiceClient = catalogServiceClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItemRequest>();

        foreach (var item in request.Items)
        {
            var product = await _catalogServiceClient.GetProductAsync(item.ProductId, cancellationToken);
            if (product is null)
                return Result<OrderDto>.Failure($"Produto {item.ProductId} não encontrado ou indisponível.");

            if (!product.Active)
                return Result<OrderDto>.Failure($"Produto '{product.Name}' não está mais disponível.");

            orderItems.Add(new OrderItemRequest(product.ProductId, product.Name, product.Price, item.Quantity));
        }

        var order = Order.Create(request.UserId, orderItems);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // ATENÇÃO — dual write deliberado.
        // Gravar no banco e publicar no broker são duas operações sem
        // transação em comum. Se o processo morrer aqui, o pedido existe
        // e o evento nunca sai: o estoque nunca é reservado e ninguém
        // sabe. Não há transação distribuída que resolva isso.
        // A Fase 6 troca este publish direto pelo Transactional Outbox:
        // o evento passa a ser gravado na MESMA transação do pedido, e
        // um worker publica depois.
        try
        {
            var integrationEvent = new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.TotalAmount,
                order.CreatedAt,
                order.Items.Select(i => new OrderCreatedItem(i.ProductId, i.ProductName, i.Quantity)).ToList());

            await _eventPublisher.PublishAsync(integrationEvent, OrderRoutingKeys.Created, cancellationToken);
        }
        catch (Exception ex)
        {
            // O pedido já está gravado: não desfazemos nada e não
            // devolvemos erro, porque o pedido realmente foi criado.
            // Logar aqui deixa a falha visível em vez de silenciosa.
            _logger.LogError(ex,
                "Pedido {OrderId} foi gravado, mas a publicação de OrderCreated falhou. O estoque não será reservado.",
                order.Id);
        }

        return Result<OrderDto>.Success(order.ToDto());
    }
}
