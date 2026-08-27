using Catalog.Domain.Products;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    private static Product NovoProduto(decimal price = 100m, int stock = 5)
        => Product.Create("Teclado", "Descrição", price, stock);

    // ---------- Create ----------

    [Fact]
    public void Create_ComDadosValidos_PreencheEstadoInicial()
    {
        var antes = DateTimeOffset.UtcNow;

        var product = Product.Create("Teclado", "RGB", 350.00m, 10);

        var depois = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Teclado", product.Name);
        Assert.Equal("RGB", product.Description);
        Assert.Equal(350.00m, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.True(product.Active);
        Assert.InRange(product.CreatedAt, antes, depois);
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public void Create_ChamadoDuasVezes_GeraIdsDistintos()
    {
        var a = NovoProduto();
        var b = NovoProduto();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Create_ComDescriptionNula_EhPermitido()
    {
        var product = Product.Create("Teclado", null, 10m, 1);

        Assert.Null(product.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ComNomeVazio_LancaArgumentException(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Product.Create(name!, null, 10m, 1));
    }

    [Fact]
    public void Create_ComPrecoNegativo_LancaArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("Teclado", null, -0.01m, 1));
    }

    [Fact]
    public void Create_ComEstoqueNegativo_LancaArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("Teclado", null, 10m, -1));
    }

    [Fact]
    public void Create_ComPrecoEEstoqueZero_EhPermitido()
    {
        var product = Product.Create("Brinde", null, 0m, 0);

        Assert.Equal(0m, product.Price);
        Assert.Equal(0, product.StockQuantity);
    }

    // ---------- UpdateDetails ----------

    [Fact]
    public void UpdateDetails_ComDadosValidos_AtualizaCamposEMarcaUpdatedAt()
    {
        var product = NovoProduto(price: 100m, stock: 7);

        product.UpdateDetails("Teclado RGB", "switch marrom", 380.00m);

        Assert.Equal("Teclado RGB", product.Name);
        Assert.Equal("switch marrom", product.Description);
        Assert.Equal(380.00m, product.Price);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_NaoAlteraEstoqueAtivacaoNemCreatedAt()
    {
        var product = NovoProduto(stock: 7);
        var createdAt = product.CreatedAt;

        product.UpdateDetails("Outro nome", null, 1m);

        Assert.Equal(7, product.StockQuantity);
        Assert.True(product.Active);
        Assert.Equal(createdAt, product.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_ComNomeVazio_LancaArgumentException(string? name)
    {
        var product = NovoProduto();

        Assert.ThrowsAny<ArgumentException>(() => product.UpdateDetails(name!, null, 10m));
    }

    [Fact]
    public void UpdateDetails_ComPrecoNegativo_LancaEDeixaEstadoIntacto()
    {
        var product = NovoProduto(price: 100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateDetails("Novo", null, -1m));

        Assert.Equal("Teclado", product.Name);
        Assert.Equal(100m, product.Price);
        Assert.Null(product.UpdatedAt);
    }

    // ---------- AdjustStock ----------

    [Fact]
    public void AdjustStock_ComQuantidadePositiva_SomaAoEstoque()
    {
        var product = NovoProduto(stock: 5);

        product.AdjustStock(3);

        Assert.Equal(8, product.StockQuantity);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void AdjustStock_ComQuantidadeNegativa_SubtraiDoEstoque()
    {
        var product = NovoProduto(stock: 5);

        product.AdjustStock(-2);

        Assert.Equal(3, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_ZerandoOEstoque_EhPermitido()
    {
        var product = NovoProduto(stock: 5);

        product.AdjustStock(-5);

        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_QueDeixariaEstoqueNegativo_LancaInvalidOperation()
    {
        var product = NovoProduto(stock: 5);

        var ex = Assert.Throws<InvalidOperationException>(() => product.AdjustStock(-6));

        Assert.Equal("Estoque não pode ficar negativo.", ex.Message);
    }

    [Fact]
    public void AdjustStock_QuandoLanca_NaoAlteraEstoqueNemUpdatedAt()
    {
        var product = NovoProduto(stock: 5);

        Assert.Throws<InvalidOperationException>(() => product.AdjustStock(-6));

        Assert.Equal(5, product.StockQuantity);
        Assert.Null(product.UpdatedAt);
    }

    // ---------- Deactivate / Activate ----------

    [Fact]
    public void Deactivate_TornaProdutoInativoEMarcaUpdatedAt()
    {
        var product = NovoProduto();

        product.Deactivate();

        Assert.False(product.Active);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void Deactivate_NaoRemoveDadosDoProduto()
    {
        var product = NovoProduto(price: 100m, stock: 5);

        product.Deactivate();

        Assert.Equal("Teclado", product.Name);
        Assert.Equal(100m, product.Price);
        Assert.Equal(5, product.StockQuantity);
    }

    [Fact]
    public void Activate_ReativaProdutoInativo()
    {
        var product = NovoProduto();
        product.Deactivate();

        product.Activate();

        Assert.True(product.Active);
    }
}
