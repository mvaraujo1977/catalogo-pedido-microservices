using Catalog.Application.Products.Commands.DeleteProduct;
using Catalog.Domain.Products;
using Catalog.UnitTests.Application.Fakes;

namespace Catalog.UnitTests.Application;

public class DeleteProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ComProdutoExistente_RetornaSucesso()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        var repository = new FakeProductRepository(product);
        var handler = new DeleteProductCommandHandler(repository);

        var result = await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Handle_FazSoftDelete_MantendoORegistro()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        var repository = new FakeProductRepository(product);
        var handler = new DeleteProductCommandHandler(repository);

        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        var aindaExiste = await repository.GetByIdAsync(product.Id, CancellationToken.None);

        Assert.NotNull(aindaExiste);
        Assert.False(aindaExiste!.Active);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ProdutoRemovido_SomeDaListagemPaginada()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        var repository = new FakeProductRepository(product);
        var handler = new DeleteProductCommandHandler(repository);

        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);
        var listagem = await repository.GetPagedAsync(1, 20, CancellationToken.None);

        Assert.Empty(listagem);
    }

    [Fact]
    public async Task Handle_ComIdInexistente_RetornaFalhaSemPersistir()
    {
        var repository = new FakeProductRepository();
        var handler = new DeleteProductCommandHandler(repository);

        var result = await handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Produto não encontrado.", result.Error);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ChamadoDuasVezes_EhIdempotenteNoEstadoFinal()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        var repository = new FakeProductRepository(product);
        var handler = new DeleteProductCommandHandler(repository);

        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);
        var segundo = await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        Assert.True(segundo.IsSuccess);
        Assert.False(product.Active);
    }
}
