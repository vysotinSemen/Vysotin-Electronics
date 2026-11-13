using Electronics.CsvImport.Models;

namespace Electronics.CsvImport.Services;

public interface IProductRepository
{
    int Count { get; }
    bool TryAdd(Product product);
    Product? GetBySku(string sku);
    IReadOnlyList<Product> GetAll();
}
