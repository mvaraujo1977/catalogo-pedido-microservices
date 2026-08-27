using Catalog.Application.Products.Commands.UpdateProduct;

namespace Catalog.UnitTests.Application.Validators;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    private static UpdateProductCommand Valido(
        Guid? id = null,
        string name = "Teclado",
        string? description = "RGB",
        decimal price = 350.00m)
        => new(id ?? Guid.NewGuid(), name, description, price);

    [Fact]
    public void ComandoValido_Passa()
    {
        var result = _validator.Validate(Valido());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void IdVazio_Falha()
    {
        var result = _validator.Validate(Valido(id: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NomeVazio_Falha(string name)
    {
        var result = _validator.Validate(Valido(name: name));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.Name));
    }

    [Fact]
    public void NomeComMaisDe200Caracteres_Falha()
    {
        var result = _validator.Validate(Valido(name: new string('a', 201)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void DescriptionComMaisDe1000Caracteres_Falha()
    {
        var result = _validator.Validate(Valido(description: new string('a', 1001)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.Description));
    }

    [Fact]
    public void PrecoNegativo_Falha()
    {
        var result = _validator.Validate(Valido(price: -0.01m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.Price));
    }

    [Fact]
    public void PrecoZero_Passa()
    {
        var result = _validator.Validate(Valido(price: 0m));

        Assert.True(result.IsValid);
    }
}
