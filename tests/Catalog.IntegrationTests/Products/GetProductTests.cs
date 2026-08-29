using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Products.Dtos;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Products;

public class GetProductTests : IntegrationTestBase
{
    public GetProductTests(CatalogApiFactory factory) : base(factory) { }

    // ---------- GET por id ----------

    [Fact]
    public async Task GetById_ComProdutoExistente_Retorna200ComODto()
    {
        var criado = await CriarProdutoAsync();

        var response = await Client.GetAsync($"/api/v1/products/{criado.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(criado.Id, dto!.Id);
        Assert.Equal(criado.Name, dto.Name);
        Assert.Equal(criado.Price, dto.Price);
    }

    [Fact]
    public async Task GetById_ComIdInexistente_Retorna400ComMensagemDeNaoEncontrado()
    {
        // Comportamento atual: o Result.Failure vira 400 no ToActionResult.
        // A Fase 8 (tratamento global de erros) deve transformar isso em 404.
        var response = await Client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problema = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("Produto não encontrado.", problema!.Title);
    }

    [Fact]
    public async Task GetById_ComIdMalFormado_Retorna404PelaRestricaoDeRota()
    {
        var response = await Client.GetAsync("/api/v1/products/nao-e-um-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET listagem ----------

    [Fact]
    public async Task GetPaged_SemProdutos_Retorna200ComListaVazia()
    {
        var response = await Client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var lista = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Empty(lista!);
    }

    [Fact]
    public async Task GetPaged_ComProdutos_RetornaTodosOsAtivos()
    {
        await CriarProdutoAsync(name: "Produto A");
        await CriarProdutoAsync(name: "Produto B");
        await CriarProdutoAsync(name: "Produto C");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");

        Assert.Equal(3, lista!.Count);
    }

    [Fact]
    public async Task GetPaged_OrdenaPorCreatedAt()
    {
        await CriarProdutoAsync(name: "Primeiro");
        await CriarProdutoAsync(name: "Segundo");
        await CriarProdutoAsync(name: "Terceiro");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");

        Assert.Equal(["Primeiro", "Segundo", "Terceiro"], lista!.Select(p => p.Name));
    }

    [Fact]
    public async Task GetPaged_RespeitaPageSize()
    {
        await CriarProdutoAsync(name: "Produto A");
        await CriarProdutoAsync(name: "Produto B");
        await CriarProdutoAsync(name: "Produto C");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products?page=1&pageSize=2");

        Assert.Equal(2, lista!.Count);
        Assert.Equal(["Produto A", "Produto B"], lista.Select(p => p.Name));
    }

    [Fact]
    public async Task GetPaged_SegundaPagina_RetornaORestante()
    {
        await CriarProdutoAsync(name: "Produto A");
        await CriarProdutoAsync(name: "Produto B");
        await CriarProdutoAsync(name: "Produto C");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products?page=2&pageSize=2");

        Assert.Equal(["Produto C"], lista!.Select(p => p.Name));
    }

    [Fact]
    public async Task GetPaged_PaginaAlemDoFim_RetornaListaVazia()
    {
        await CriarProdutoAsync();

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products?page=99&pageSize=20");

        Assert.Empty(lista!);
    }

    [Fact]
    public async Task GetPaged_NaoRetornaProdutosInativos()
    {
        var ativo = await CriarProdutoAsync(name: "Ativo");
        var inativo = await CriarProdutoAsync(name: "Inativo");

        await Client.DeleteAsync($"/api/v1/products/{inativo.Id}");

        var lista = await Client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");

        Assert.Equal(["Ativo"], lista!.Select(p => p.Name));
        Assert.DoesNotContain(lista!, p => p.Id == inativo.Id);
        Assert.Contains(lista!, p => p.Id == ativo.Id);
    }
}
