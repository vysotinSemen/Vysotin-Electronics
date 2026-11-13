namespace Electronics.CsvImport.Models;

public sealed record Product
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "RUB";
    public string CategoryName { get; init; } = "Без категории";
    public int StockQuantity { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string Brand { get; init; } = string.Empty;
    public int WarrantyMonths { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
}
