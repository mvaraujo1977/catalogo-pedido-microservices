using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Orders.Application.Common;
using Orders.Application.Orders.Commands.CancelOrder;
using Orders.Application.Orders.Commands.CompleteOrder;
using Orders.Application.Orders.Commands.ConfirmOrder;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Application.Orders;
using Orders.Application.Orders.Dtos;
using Orders.Application.Orders.Queries.GetOrderById;
using Orders.Application.Orders.Queries.GetOrders;

namespace Orders.API.Controllers;

public record CreateOrderRequestBody(IReadOnlyList<CreateOrderItemRequest> Items);

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [Authorize(Policy = "CustomerOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequestBody body, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Unauthorized();

        var command = new CreateOrderCommand(_currentUser.UserId.Value, body.Items);
        var result = await _mediator.Send(command, cancellationToken);
        return ToActionResult(result, r => CreatedAtAction(nameof(GetById), new { id = r.Id }, r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return ToOrderActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrdersQuery(null, page, pageSize), cancellationToken);
        return ToActionResult(result, Ok);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConfirmOrderCommand(id), cancellationToken);
        return ToOrderActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id), cancellationToken);
        return ToOrderActionResult(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteOrderCommand(id), cancellationToken);
        return ToOrderActionResult(result);
    }

    private IActionResult ToOrderActionResult(Result<OrderDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        // Mensagem específica de "não encontrado/não é seu" vira 404
        // (esconde a existência do pedido de outro usuário). Qualquer
        // outra falha (ex: transição de estado inválida) segue o
        // padrão 400 do resto do projeto.
        if (result.Error == OrderErrors.NotFoundOrNotOwned)
            return NotFound(new ProblemDetails { Title = result.Error });

        return BadRequest(new ProblemDetails { Title = result.Error });
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });

        return onSuccess(result.Value!);
    }
}
