using Catalog.Application.Common;
using Catalog.Application.Products.Commands.CreateProduct;
using Catalog.Application.Products.Commands.DeleteProduct;
using Catalog.Application.Products.Commands.UpdateProduct;
using Catalog.Application.Products.Dtos;
using Catalog.Application.Products.Queries.GetProductById;
using Catalog.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return ToActionResult(result, r => CreatedAtAction(nameof(GetById), new { id = r.Id }, r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return ToActionResult(result, Ok);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize), cancellationToken);
        return ToActionResult(result, Ok);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new ProblemDetails { Title = "Id da rota difere do Id do corpo da requisição." });

        var result = await _mediator.Send(command, cancellationToken);
        return ToActionResult(result, Ok);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return ToActionResult(result, () => NoContent());
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        // Mapeamento único e centralizado Result -> HTTP.
        // Hoje: falha sempre vira 400. Na Fase 8 (tratamento global de
        // erros), este é o único lugar que precisará mudar para
        // diferenciar 404 / 400 / 409 por tipo de falha.
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });

        return onSuccess(result.Value!);
    }

    private IActionResult ToActionResult(Result result, Func<IActionResult> onSuccess)
    {
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });

        return onSuccess();
    }
}
