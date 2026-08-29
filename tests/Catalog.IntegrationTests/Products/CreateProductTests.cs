using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Products.Dtos;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Products;

public class CreateProductTests : IntegrationTestBase
{
    public CreateProductTests(CatalogApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Post_ComDadosValidos_Retorna201ComCorpoPreenchido()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Teclado Mecânico",
            description = "RGB, switch azul",
            price = 350.00m,
            stockQuantity = 10
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.Id);
        Assert.Equal("Teclado Mecânico", dto.Name);
        Assert.Equal("RGB, switch azul", dto.Description);
        Assert.Equal(350.00m, dto.Price);
        Assert.Equal(10, dto.StockQuantity);
        Assert.True(dto.Active);
        Assert.Null(dto.UpdatedAt);
    }

    [Fact]
    public async Task Post_Retorna201ComHeaderLocationApontandoParaORecurso()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Mouse",
            description = (string?)null,
            price = 90.00m,
            stockQuantity = 3
        });

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/products/{dto!.Id}", response.Headers.Location!.ToString());

        // O Location precisa ser navegável de fato.
        var seguindoLocation = await Client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, seguindoLocation.StatusCode);
    }

    [Fact]
    public async Task Post_PersisteORegistroNoBanco()
    {
        var dto = await CriarProdutoAsync(name: "Monitor", price: 1299.90m, stockQuantity: 4);

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.Id));

        Assert.NotNull(doBanco);
        Assert.Equal("Monitor", doBanco!.Name);
        Assert.Equal(1299.90m, doBanco.Price);
        Assert.Equal(4, doBanco.StockQuantity);
        Assert.True(doBanco.Active);
    }

    [Fact]
    public async Task Post_ComDescriptionNula_EhAceito()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Cabo HDMI",
            description = (string?)null,
            price = 39.90m,
            stockQuantity = 100
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Null(dto!.Description);
    }

    [Fact]
    public async Task Post_ComPrecoEEstoqueZero_EhAceito()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Brinde",
            description = (string?)null,
            price = 0m,
            stockQuantity = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_PreservaDuasCasasDecimaisNoPreco()
    {
        var dto = await CriarProdutoAsync(price: 1234.56m);

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstAsync(p => p.Id == dto.Id));

        Assert.Equal(1234.56m, doBanco.Price);
    }

    [Fact]
    public async Task Post_ComAcentuacao_PreservaOsCaracteres()
    {
        var dto = await CriarProdutoAsync(name: "Cadeira Ergonômica", description: "Ajuste lombar e apoio de braço");

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstAsync(p => p.Id == dto.Id));

        Assert.Equal("Cadeira Ergonômica", doBanco.Name);
        Assert.Equal("Ajuste lombar e apoio de braço", doBanco.Description);
    }

    [Fact]
    public async Task Post_DuasVezes_GeraIdsDistintos()
    {
        var primeiro = await CriarProdutoAsync(name: "Produto A");
        var segundo = await CriarProdutoAsync(name: "Produto B");

        Assert.NotEqual(primeiro.Id, segundo.Id);
    }
}
