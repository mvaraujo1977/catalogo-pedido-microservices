using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Products;

/// <summary>
/// GAP CONHECIDO: AddApplication registra os validators do FluentValidation
/// (AddValidatorsFromAssembly), mas nada os executa — não há pipeline behavior
/// do MediatR nem AddFluentValidationAutoValidation. Na prática a entrada
/// inválida atravessa a Application, estoura no domínio e vira 500.
///
/// Os testes abaixo fixam esse comportamento como ele é hoje. Quando a Fase 8
/// ligar a validação e o tratamento global de erros, eles falham de propósito e
/// devem ser reescritos para esperar 400 com os erros de validação.
/// </summary>
public class ProductValidationTests : IntegrationTestBase
{
    public ProductValidationTests(CatalogApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Post_ComNomeVazio_HojeRetorna500_PorqueOsValidatorsNaoRodam()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "",
            description = (string?)null,
            price = 10m,
            stockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComPrecoNegativo_HojeRetorna500_PorqueOsValidatorsNaoRodam()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Teclado",
            description = (string?)null,
            price = -5m,
            stockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComEstoqueNegativo_HojeRetorna500_PorqueOsValidatorsNaoRodam()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Teclado",
            description = (string?)null,
            price = 10m,
            stockQuantity = -1
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComNomeAcimaDe200Caracteres_HojePassa_PorqueOLimiteSoExisteNoValidator()
    {
        // O limite de 200 é só do validator (que não roda) e da coluna do banco.
        // A coluna é varchar(200), então quem barra é o Postgres — com 500, não 400.
        var response = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = new string('a', 201),
            description = (string?)null,
            price = 10m,
            stockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComEntradaInvalida_NaoPersisteNadaNoBanco()
    {
        await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "",
            description = (string?)null,
            price = 10m,
            stockQuantity = 1
        });

        var total = await Factory.QueryDatabaseAsync(db => db.Products.CountAsync());

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task Put_ComNomeVazio_HojeRetorna500_PorqueOsValidatorsNaoRodam()
    {
        var criado = await CriarProdutoAsync();

        var response = await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = "",
            description = (string?)null,
            price = 10m
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Put_ComEntradaInvalida_NaoAlteraOProdutoNoBanco()
    {
        var criado = await CriarProdutoAsync(name: "Original");

        await Client.PutAsJsonAsync($"/api/v1/products/{criado.Id}", new
        {
            id = criado.Id,
            name = "",
            description = (string?)null,
            price = 10m
        });

        var doBanco = await Factory.QueryDatabaseAsync(db =>
            db.Products.AsNoTracking().FirstAsync(p => p.Id == criado.Id));

        Assert.Equal("Original", doBanco.Name);
        Assert.Null(doBanco.UpdatedAt);
    }
}
