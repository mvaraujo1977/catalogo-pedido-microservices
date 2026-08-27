using Catalog.Application.Products;
using Catalog.Domain.Products;

namespace Catalog.UnitTests.Application.Fakes;

/// <summary>
/// Implementação in-memory de <see cref="IProductRepository"/> para os testes
/// de handler. Registra as chamadas recebidas para que os testes possam
/// afirmar sobre o comportamento, não só sobre o retorno.
/// </summary>
public class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();

    public IReadOnlyList<Product> Added => _addedProducts;
    private readonly List<Product> _addedProducts = new();

    public int SaveChangesCallCount { get; private set; }
    public int? LastPageRequested { get; private set; }
    public int? LastPageSizeRequested { get; private set; }

    public FakeProductRepository(params Product[] seed) => _products.AddRange(seed);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        LastPageRequested = page;
        LastPageSizeRequested = pageSize;

        IReadOnlyList<Product> result = _products
            .Where(p => p.Active)
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(result);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _products.Add(product);
        _addedProducts.Add(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
