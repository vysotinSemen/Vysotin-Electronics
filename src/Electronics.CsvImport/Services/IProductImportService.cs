using Electronics.CsvImport.Models;

namespace Electronics.CsvImport.Services;

public interface IProductImportService
{
    ImportResult ImportFromCsv(string? filePath);
    ImportResult ImportFromLines(IEnumerable<string> lines);
}
