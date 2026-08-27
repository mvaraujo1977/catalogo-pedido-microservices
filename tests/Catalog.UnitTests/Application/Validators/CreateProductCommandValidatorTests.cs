using Catalog.Application.Products.Commands.CreateProduct;

namespace Catalog.UnitTests.Application.Validators;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand Valido(
        string name = "Teclado",
        string? description = "RGB",
        decimal price = 350.00m,
        int stockQuantity = 10)
        => new(name, description, price, stockQuantity);

    [Fact]
    public void ComandoValido_Passa()
    {
        var result = _validator.Validate(Valido());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NomeVazio_Falha(string name)
    {
        var result = _validator.Validate(Valido(name: name));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void NomeComMaisDe200Caracteres_Falha()
    {
        var result = _validator.Validate(Valido(name: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void NomeCom200Caracteres_Passa()
    {
        var result = _validator.Validate(Valido(name: new string('a', 200)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DescriptionComMaisDe1000Caracteres_Falha()
    {
        var result = _validator.Validate(Valido(description: new string('a', 1001)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Description));
    }

    [Fact]
    public void DescriptionNula_Passa()
    {
        var result = _validator.Validate(Valido(description: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PrecoNegativo_Falha()
    {
        var result = _validator.Validate(Valido(price: -0.01m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Price));
    }

    [Fact]
    public void PrecoZero_Passa()
    {
        var result = _validator.Validate(Valido(price: 0m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EstoqueNegativo_Falha()
    {
        var result = _validator.Validate(Valido(stockQuantity: -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.StockQuantity));
    }

    [Fact]
    public void EstoqueZero_Passa()
    {
        var result = _validator.Validate(Valido(stockQuantity: 0));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void VariosCamposInvalidos_ReportaTodosOsErros()
    {
        var result = _validator.Validate(new CreateProductCommand("", null, -1m, -1));

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Select(e => e.PropertyName).Distinct().Count());
    }
}
