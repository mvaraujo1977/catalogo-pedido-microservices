using Catalog.Application.Products.Queries.GetProductById;
using Catalog.Domain.Products;
using Catalog.UnitTests.Application.Fakes;

namespace Catalog.UnitTests.Application;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ComProdutoExistente_RetornaDto()
    {
        var product = Product.Create("Teclado", "RGB", 350.00m, 10);
        var repository = new FakeProductRepository(product);
        var handler = new GetProductByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(product.Id, result.Value!.Id);
        Assert.Equal("Teclado", result.Value.Name);
        Assert.Equal(350.00m, result.Value.Price);
    }

    [Fact]
    public async Task Handle_ComIdInexistente_RetornaFalha()
    {
        var repository = new FakeProductRepository();
        var handler = new GetProductByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Produto não encontrado.", result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Handle_ComProdutoInativo_AindaRetornaODto()
    {
        // Busca por id não filtra por Active — inativo continua acessível
        // diretamente, ao contrário da listagem paginada.
        var product = Product.Create("Teclado", null, 10m, 1);
        product.Deactivate();
        var repository = new FakeProductRepository(product);
        var handler = new GetProductByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Active);
    }

    [Fact]
    public async Task Handle_NaoEscreveNoRepositorio()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        var repository = new FakeProductRepository(product);
        var handler = new GetProductByIdQueryHandler(repository);

        await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.Equal(0, repository.SaveChangesCallCount);
    }
}
