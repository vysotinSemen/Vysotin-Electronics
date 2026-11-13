namespace Electronics.CsvImport.Models;

public sealed class ImportViewModel
{
    public string FilePath { get; init; } = "Import/sample_products.csv";
    public ImportResult? Result { get; init; }
}

public sealed record CatalogViewModel(IReadOnlyList<Product> Products, StartupImportReport Startup);
