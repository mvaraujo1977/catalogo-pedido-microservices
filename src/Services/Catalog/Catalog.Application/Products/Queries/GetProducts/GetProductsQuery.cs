using Catalog.Application.Common;
using Catalog.Application.Products.Dtos;
using MediatR;

namespace Catalog.Application.Products.Queries.GetProducts;

public record GetProductsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<IReadOnlyList<ProductDto>>>;
