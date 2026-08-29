using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Products.Dtos;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Products;

public class DeleteProductTests : IntegrationTestBase
{
    public DeleteProductTests(CatalogApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Delete_ComProdutoExistente_Retorna204()
    {
        var criado = await CriarProdutoAsync();

        var response = await Client.DeleteAsync($"/api/v1/products/{criado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_FazSoftDelete_MantendoORegistroNoBanco()
    {
        var criado = await CriarProdutoAsync();

        await Client.DeleteAsync($"/api/v1/products/{criado.Id}");

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == criado.Id));

        Assert.NotNull(doBanco);
        Assert.False(doBanco!.Active);
        Assert.NotNull(doBanco.UpdatedAt);
    }

    [Fact]
    public async Task Delete_NaoApagaOsDadosDoProduto()
    {
        var criado = await CriarProdutoAsync(name: "Teclado", price: 350.00m, stockQuantity: 10);

        await Client.DeleteAsync($"/api/v1/products/{criado.Id}");

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstAsync(p => p.Id == criado.Id));

        Assert.Equal("Teclado", doBanco.Name);
        Assert.Equal(350.00m, doBanco.Price);
        Assert.Equal(10, doBanco.StockQuantity);
    }

    [Fact]
    public async Task Delete_RemoveOProdutoDaListagemPaginada()
    {
        var criado = await CriarProdutoAsync();

        await Client.DeleteAsync($"/api/v1/products/{criado.Id}");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");

        Assert.Empty(lista!);
    }

    [Fact]
    public async Task Delete_MantemOProdutoAcessivelPorId()
    {
        // Comportamento atual: GET por id nao filtra por Active, entao um produto
        // inativo continua retornando 200. Se a regra mudar, este teste falha de proposito.
        var criado = await CriarProdutoAsync();

        await Client.DeleteAsync($"/api/v1/products/{criado.Id}");

        var response = await Client.GetAsync($"/api/v1/products/{criado.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.False(dto!.Active);
    }

    [Fact]
    public async Task Delete_ComIdInexistente_Retorna400ComMensagemDeNaoEncontrado()
    {
        var response = await Client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problema = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("Produto não encontrado.", problema!.Title);
    }

    [Fact]
    public async Task Delete_ChamadoDuasVezes_ContinuaRetornando204()
    {
        var criado = await CriarProdutoAsync();

        var primeiro = await Client.DeleteAsync($"/api/v1/products/{criado.Id}");
        var segundo = await Client.DeleteAsync($"/api/v1/products/{criado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segundo.StatusCode);
    }
}
