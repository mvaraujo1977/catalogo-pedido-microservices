using Catalog.Application.Products.Queries.GetProducts;
using Catalog.Domain.Products;
using Catalog.UnitTests.Application.Fakes;

namespace Catalog.UnitTests.Application;

public class GetProductsQueryHandlerTests
{
    private static Product[] TresProdutos() =>
    [
        Product.Create("A", null, 1m, 1),
        Product.Create("B", null, 2m, 2),
        Product.Create("C", null, 3m, 3)
    ];

    [Fact]
    public async Task Handle_SemProdutos_RetornaListaVaziaComSucesso()
    {
        var handler = new GetProductsQueryHandler(new FakeProductRepository());

        var result = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_MapeiaTodosOsProdutosParaDto()
    {
        var repository = new FakeProductRepository(TresProdutos());
        var handler = new GetProductsQueryHandler(repository);

        var result = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        Assert.Equal(3, result.Value!.Count);
        Assert.Equal(["A", "B", "C"], result.Value.Select(p => p.Name));
    }

    [Fact]
    public async Task Handle_UsaPaginacaoPadraoQuandoNaoInformada()
    {
        var repository = new FakeProductRepository();
        var handler = new GetProductsQueryHandler(repository);

        await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        Assert.Equal(1, repository.LastPageRequested);
        Assert.Equal(20, repository.LastPageSizeRequested);
    }

    [Fact]
    public async Task Handle_RepassaPageEPageSizeAoRepositorio()
    {
        var repository = new FakeProductRepository();
        var handler = new GetProductsQueryHandler(repository);

        await handler.Handle(new GetProductsQuery(3, 5), CancellationToken.None);

        Assert.Equal(3, repository.LastPageRequested);
        Assert.Equal(5, repository.LastPageSizeRequested);
    }

    [Fact]
    public async Task Handle_ComPageSizeMenorQueOTotal_RetornaApenasAPagina()
    {
        var repository = new FakeProductRepository(TresProdutos());
        var handler = new GetProductsQueryHandler(repository);

        var result = await handler.Handle(new GetProductsQuery(1, 2), CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task Handle_NaoRetornaProdutosInativos()
    {
        var produtos = TresProdutos();
        produtos[1].Deactivate();
        var repository = new FakeProductRepository(produtos);
        var handler = new GetProductsQueryHandler(repository);

        var result = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
        Assert.DoesNotContain(result.Value, p => p.Name == "B");
    }

    [Fact]
    public async Task Handle_NaoEscreveNoRepositorio()
    {
        var repository = new FakeProductRepository(TresProdutos());
        var handler = new GetProductsQueryHandler(repository);

        await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        Assert.Equal(0, repository.SaveChangesCallCount);
    }
}
