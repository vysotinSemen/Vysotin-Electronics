using Electronics.CsvImport.Models;

namespace Electronics.CsvImport.Services;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<string, Product> _products = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public int Count
    {
        get { lock (_sync) return _products.Count; }
    }

    public bool TryAdd(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentException.ThrowIfNullOrWhiteSpace(product.Sku);
        var normalized = product with { Sku = product.Sku.Trim() };
        lock (_sync)
            return _products.TryAdd(normalized.Sku, normalized);
    }

    public Product? GetBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return null;
        lock (_sync)
            return _products.GetValueOrDefault(sku.Trim());
    }

    public IReadOnlyList<Product> GetAll()
    {
        lock (_sync)
            return Array.AsReadOnly(_products.Values.ToArray());
    }
}
