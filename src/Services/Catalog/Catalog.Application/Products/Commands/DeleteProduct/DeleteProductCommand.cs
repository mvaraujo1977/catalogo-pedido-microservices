using Catalog.Application.Common;
using MediatR;

namespace Catalog.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<Result>;
