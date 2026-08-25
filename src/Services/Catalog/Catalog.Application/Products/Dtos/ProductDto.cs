namespace Catalog.Application.Products.Dtos;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    bool Active,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
