using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Products.Dtos;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Products;

public class UpdateProductTests : IntegrationTestBase
{
    public UpdateProductTests(CatalogApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Put_ComDadosValidos_Retorna200ComDtoAtualizado()
    {
        var criado = await CriarProdutoAsync();

        var response = await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = "Teclado Mecânico RGB",
            description = "RGB, switch marrom",
            price = 380.00m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(criado.Id, dto!.Id);
        Assert.Equal("Teclado Mecânico RGB", dto.Name);
        Assert.Equal("RGB, switch marrom", dto.Description);
        Assert.Equal(380.00m, dto.Price);
        Assert.NotNull(dto.UpdatedAt);
    }

    [Fact]
    public async Task Put_PersisteAsAlteracoesNoBanco()
    {
        var criado = await CriarProdutoAsync();

        await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = "Nome novo",
            description = "Descrição nova",
            price = 999.99m
        });

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstAsync(p => p.Id == criado.Id));

        Assert.Equal("Nome novo", doBanco.Name);
        Assert.Equal("Descrição nova", doBanco.Description);
        Assert.Equal(999.99m, doBanco.Price);
        Assert.NotNull(doBanco.UpdatedAt);
    }

    [Fact]
    public async Task Put_NaoAlteraEstoqueCreatedAtNemAtivacao()
    {
        var criado = await CriarProdutoAsync(stockQuantity: 42);

        var response = await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = "Outro nome",
            description = (string?)null,
            price = 1m
        });

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(42, dto!.StockQuantity);
        Assert.True(dto.Active);

        // O POST devolve o objeto em memoria (ticks de 100ns); toda leitura
        // posterior vem do Postgres, cujo timestamptz trunca em microssegundo.
        // Comparar os dois exige tolerar essa diferenca nos ultimos digitos.
        Assert.True((criado.CreatedAt - dto.CreatedAt).Duration() < TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Put_ComIdDaRotaDiferenteDoCorpo_Retorna400()
    {
        var criado = await CriarProdutoAsync();

        var response = await Client.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}", new
        {
            id = criado.Id,
            name = "Teclado",
            description = (string?)null,
            price = 10m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problema = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("Id da rota difere do Id do corpo da requisição.", problema!.Title);
    }

    [Fact]
    public async Task Put_ComIdDivergente_NaoAlteraOProdutoExistente()
    {
        var criado = await CriarProdutoAsync(name: "Original");

        await Client.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}", new
        {
            id = criado.Id,
            name = "Nao deve ser aplicado",
            description = (string?)null,
            price = 1m
        });

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstAsync(p => p.Id == criado.Id));

        Assert.Equal("Original", doBanco.Name);
        Assert.Null(doBanco.UpdatedAt);
    }

    [Fact]
    public async Task Put_ComIdInexistente_Retorna400ComMensagemDeNaoEncontrado()
    {
        var id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync($"/api/v1/products/{id}", new
        {
            id,
            name = "Teclado",
            description = (string?)null,
            price = 10m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problema = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("Produto não encontrado.", problema!.Title);
    }

    [Fact]
    public async Task Put_LimpandoDescription_GravaNull()
    {
        var criado = await CriarProdutoAsync(description: "tinha descricao");

        var response = await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = criado.Name,
            description = (string?)null,
            price = criado.Price
        });

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Null(dto!.Description);
    }
}
