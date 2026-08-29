using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Products.Dtos;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Products;

/// <summary>
/// Percorre o ciclo de vida completo de um produto atravessando todas as
/// camadas: controller -> MediatR -> handler -> repositorio -> EF Core -> Postgres.
/// </summary>
public class ProductLifecycleTests : IntegrationTestBase
{
    public ProductLifecycleTests(CatalogApiFactory factory) : base(factory) { }

    [Fact]
    public async Task CicloCompleto_CriarLerAtualizarRemover()
    {
        // 1. cria
        var criado = await CriarProdutoAsync(name: "Teclado", price: 350.00m, stockQuantity: 10);
        Assert.True(criado.Active);
        Assert.Null(criado.UpdatedAt);

        // 2. le por id
        var lido = await Client.GetFromJsonAsync<ProductDto>($"/api/v1/products/{criado.Id}");
        Assert.Equal(criado.Id, lido!.Id);

        // 3. aparece na listagem
        var listaAntes = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");
        Assert.Contains(listaAntes!, p => p.Id == criado.Id);

        // 4. atualiza
        var putResponse = await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = "Teclado RGB",
            description = "switch marrom",
            price = 380.00m
        });
        var atualizado = await putResponse.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Teclado RGB", atualizado!.Name);
        Assert.NotNull(atualizado.UpdatedAt);

        // 5. remove (soft delete)
        var deleteResponse = await Client.DeleteAsync($"/api/v1/products/{criado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 6. sumiu da listagem, mas o registro continua no banco
        var listaDepois = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");
        Assert.DoesNotContain(listaDepois!, p => p.Id == criado.Id);

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == criado.Id));
        Assert.NotNull(doBanco);
        Assert.False(doBanco!.Active);
        Assert.Equal("Teclado RGB", doBanco.Name);
    }

    [Fact]
    public async Task VariosProdutos_SaoIsoladosEntreSi()
    {
        var a = await CriarProdutoAsync(name: "Produto A", price: 10m, stockQuantity: 1);
        var b = await CriarProdutoAsync(name: "Produto B", price: 20m, stockQuantity: 2);

        await Client.DeleteAsync($"/api/v1/products/{a.Id}");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");

        Assert.Single(lista!);
        Assert.Equal(b.Id, lista![0].Id);

        var bIntacto = await Client.GetFromJsonAsync<ProductDto>($"/api/v1/products/{b.Id}");
        Assert.Equal("Produto B", bIntacto!.Name);
        Assert.True(bIntacto.Active);
        Assert.Equal(20m, bIntacto.Price);
    }

    [Fact]
    public async Task BancoEhLimpoEntreTestes()
    {
        // Garante que o ResetDatabaseAsync do IntegrationTestBase esta fazendo efeito:
        // nenhum teste anterior deixa residuo visivel aqui.
        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");

        Assert.Empty(lista!);
    }

    [Fact]
    public async Task MigrationFoiAplicada_TabelaProductsExisteComOsIndices()
    {
        var indices = await Factory.QueryDatabaseAsync(async db =>
        {
            var conn = db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT indexname FROM pg_indexes WHERE tablename = 'products' ORDER BY indexname;";

            var nomes = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                nomes.Add(reader.GetString(0));
            }

            return nomes;
        });

        Assert.Contains("PK_products", indices);
        Assert.Contains("IX_products_Name", indices);
        Assert.Contains("IX_products_Active", indices);
    }
}
