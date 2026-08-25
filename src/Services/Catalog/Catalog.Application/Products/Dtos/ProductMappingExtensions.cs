using Catalog.Domain.Products;

namespace Catalog.Application.Products.Dtos;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) =>
        new(product.Id, product.Name, product.Description, product.Price,
            product.StockQuantity, product.Active, product.CreatedAt, product.UpdatedAt);
}
