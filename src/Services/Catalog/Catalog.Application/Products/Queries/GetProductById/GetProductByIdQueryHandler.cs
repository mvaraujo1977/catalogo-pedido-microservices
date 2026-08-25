using Catalog.Application.Common;
using Catalog.Application.Products.Dtos;
using MediatR;

namespace Catalog.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result<ProductDto>.Failure("Produto não encontrado.");

        return Result<ProductDto>.Success(product.ToDto());
    }
}
