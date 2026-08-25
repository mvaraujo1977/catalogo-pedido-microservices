using Catalog.Application.Common;
using Catalog.Application.Products.Dtos;
using MediatR;

namespace Catalog.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity) : IRequest<Result<ProductDto>>;
