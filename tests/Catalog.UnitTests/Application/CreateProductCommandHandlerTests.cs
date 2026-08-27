using Catalog.Application.Products.Commands.CreateProduct;
using Catalog.UnitTests.Application.Fakes;

namespace Catalog.UnitTests.Application;

public class CreateProductCommandHandlerTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests() => _handler = new CreateProductCommandHandler(_repository);

    [Fact]
    public async Task Handle_ComComandoValido_RetornaSucessoComDtoPreenchido()
    {
        var command = new CreateProductCommand("Teclado", "RGB", 350.00m, 10);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
        Assert.Equal("Teclado", result.Value.Name);
        Assert.Equal("RGB", result.Value.Description);
        Assert.Equal(350.00m, result.Value.Price);
        Assert.Equal(10, result.Value.StockQuantity);
        Assert.True(result.Value.Active);
        Assert.Null(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Handle_PersisteOProdutoEConfirmaAsMudancas()
    {
        var command = new CreateProductCommand("Teclado", null, 10m, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        var adicionado = Assert.Single(_repository.Added);
        Assert.Equal(result.Value!.Id, adicionado.Id);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ProdutoCriadoFicaRecuperavelPeloRepositorio()
    {
        var command = new CreateProductCommand("Teclado", null, 10m, 1);

        var result = await _handler.Handle(command, CancellationToken.None);
        var recuperado = await _repository.GetByIdAsync(result.Value!.Id, CancellationToken.None);

        Assert.NotNull(recuperado);
        Assert.Equal("Teclado", recuperado!.Name);
    }

    [Fact]
    public async Task Handle_ComNomeInvalido_PropagaExcecaoDoDominio()
    {
        // O handler não valida: quem barra é o domínio (ou o validator, antes).
        var command = new CreateProductCommand("   ", null, 10m, 1);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }
}
