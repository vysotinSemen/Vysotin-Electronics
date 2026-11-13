namespace Electronics.CsvImport.Models;

public sealed class CsvImportOptions
{
    public string DirectoryPath { get; set; } = "Import";
    public string FileName { get; set; } = "sample_products.csv";
}

public sealed record StartupImportReport(string FilePath, ImportResult Result);
