using Catalog.Application.Products.Commands.UpdateProduct;
using Catalog.Domain.Products;
using Catalog.UnitTests.Application.Fakes;

namespace Catalog.UnitTests.Application;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ComProdutoExistente_AtualizaERetornaSucesso()
    {
        var product = Product.Create("Teclado", "RGB", 350.00m, 10);
        var repository = new FakeProductRepository(product);
        var handler = new UpdateProductCommandHandler(repository);

        var command = new UpdateProductCommand(product.Id, "Teclado RGB", "switch marrom", 380.00m);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Teclado RGB", result.Value!.Name);
        Assert.Equal("switch marrom", result.Value.Description);
        Assert.Equal(380.00m, result.Value.Price);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Handle_ComProdutoExistente_PreservaEstoqueECreatedAt()
    {
        var product = Product.Create("Teclado", "RGB", 350.00m, 10);
        var createdAt = product.CreatedAt;
        var repository = new FakeProductRepository(product);
        var handler = new UpdateProductCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, "Outro", null, 1m), CancellationToken.None);

        Assert.Equal(10, result.Value!.StockQuantity);
        Assert.Equal(createdAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_ComProdutoExistente_ConfirmaAsMudancas()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        var repository = new FakeProductRepository(product);
        var handler = new UpdateProductCommandHandler(repository);

        await handler.Handle(new UpdateProductCommand(product.Id, "Novo", null, 2m), CancellationToken.None);

        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ComIdInexistente_RetornaFalhaSemPersistir()
    {
        var repository = new FakeProductRepository();
        var handler = new UpdateProductCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), "Teclado", null, 10m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Produto não encontrado.", result.Error);
        Assert.Null(result.Value);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ComProdutoInativo_AindaPermiteAtualizar()
    {
        var product = Product.Create("Teclado", null, 10m, 1);
        product.Deactivate();
        var repository = new FakeProductRepository(product);
        var handler = new UpdateProductCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, "Teclado v2", null, 20m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Active);
        Assert.Equal("Teclado v2", result.Value.Name);
    }
}
