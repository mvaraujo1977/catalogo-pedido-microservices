using Catalog.Domain.Common;

namespace Catalog.Domain.Products;

public class Product : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product() { } // uso exclusivo do EF Core

    private Product(Guid id, string name, string? description, decimal price, int stockQuantity)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        Active = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Product Create(string name, string? description, decimal price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        return new Product(Guid.NewGuid(), name, description, price, stockQuantity);
    }

    public void UpdateDetails(string name, string? description, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AdjustStock(int quantity)
    {
        var newQuantity = StockQuantity + quantity;
        if (newQuantity < 0)
            throw new InvalidOperationException("Estoque não pode ficar negativo.");

        StockQuantity = newQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Active = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Active = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
