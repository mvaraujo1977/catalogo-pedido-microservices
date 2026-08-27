using Catalog.Application.Products.Dtos;
using Catalog.Domain.Products;

namespace Catalog.UnitTests.Application;

public class ProductMappingExtensionsTests
{
    [Fact]
    public void ToDto_CopiaTodosOsCamposDoProduto()
    {
        var product = Product.Create("Teclado", "RGB", 350.00m, 10);

        var dto = product.ToDto();

        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(product.Name, dto.Name);
        Assert.Equal(product.Description, dto.Description);
        Assert.Equal(product.Price, dto.Price);
        Assert.Equal(product.StockQuantity, dto.StockQuantity);
        Assert.Equal(product.Active, dto.Active);
        Assert.Equal(product.CreatedAt, dto.CreatedAt);
        Assert.Equal(product.UpdatedAt, dto.UpdatedAt);
    }

    [Fact]
    public void ToDto_ComDescriptionNula_MantemNull()
    {
        var product = Product.Create("Teclado", null, 10m, 1);

        var dto = product.ToDto();

        Assert.Null(dto.Description);
    }

    [Fact]
    public void ToDto_AposAlteracoes_RefleteEstadoAtual()
    {
        var product = Product.Create("Teclado", "RGB", 350.00m, 10);
        product.UpdateDetails("Teclado RGB", "switch marrom", 380.00m);
        product.Deactivate();

        var dto = product.ToDto();

        Assert.Equal("Teclado RGB", dto.Name);
        Assert.Equal(380.00m, dto.Price);
        Assert.False(dto.Active);
        Assert.NotNull(dto.UpdatedAt);
    }
}
