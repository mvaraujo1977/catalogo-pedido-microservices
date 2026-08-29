namespace Orders.Application.Products;

/// <summary>
/// Contrato próprio do Orders para o que ele precisa saber sobre um
/// produto — não referencia Catalog.Application. Cada bounded context
/// define seu próprio contrato, mesmo que pareça duplicar ProductDto.
/// </summary>
public record CatalogProductInfo(Guid ProductId, string Name, decimal Price, bool Active);

public interface ICatalogServiceClient
{
    Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}
