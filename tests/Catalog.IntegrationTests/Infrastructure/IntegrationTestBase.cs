using System.Net.Http.Json;
using Catalog.Application.Products.Dtos;

namespace Catalog.IntegrationTests.Infrastructure;

[Collection(nameof(CatalogApiCollection))]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly CatalogApiFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CatalogApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Cria um produto pela API e devolve o DTO retornado.</summary>
    protected async Task<ProductDto> CriarProdutoAsync(
        string name = "Teclado Mecânico",
        string? description = "RGB, switch azul",
        decimal price = 350.00m,
        int stockQuantity = 10)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/products",
            new { name, description, price, stockQuantity });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}
