using Catalog.Application.Common;
using Catalog.Application.Products.Dtos;
using MediatR;

namespace Catalog.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    public UpdateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result<ProductDto>.Failure("Produto não encontrado.");

        product.UpdateDetails(request.Name, request.Description, request.Price);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductDto>.Success(product.ToDto());
    }
}
