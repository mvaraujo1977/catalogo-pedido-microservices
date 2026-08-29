using System.Net.Http.Json;
using Orders.Application.Products;

namespace Orders.Infrastructure.Clients;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/products/{productId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<CatalogProductResponse>(cancellationToken: cancellationToken);
            if (dto is null)
                return null;

            return new CatalogProductInfo(dto.Id, dto.Name, dto.Price, dto.Active);
        }
        catch (HttpRequestException)
        {
            // Catalog indisponível: falha honesta, sem retry (decisão
            // já tomada nesta fase — sem Polly por enquanto).
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private record CatalogProductResponse(Guid Id, string Name, decimal Price, bool Active);
}
