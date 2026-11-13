using Electronics.CsvImport.Models;

namespace Electronics.CsvImport.Services;

public sealed class StartupImportService(IProductImportService importService, CsvImportOptions options)
{
    public StartupImportReport Report { get; private set; } = new(string.Empty, new ImportResult());
    private bool _hasRun;

    public StartupImportReport Run()
    {
        if (_hasRun) return Report;

        if (string.IsNullOrWhiteSpace(options.FileName))
        {
            var result = new ImportResult();
            result.Errors.Add(new(0, "В настройке CsvImport:FileName не указано имя CSV-файла."));
            Report = new(string.Empty, result);
        }
        else
        {
            var filePath = Path.Combine(options.DirectoryPath ?? string.Empty, options.FileName);
            Report = new(filePath, importService.ImportFromCsv(filePath));
        }

        _hasRun = true;
        return Report;
    }
}
