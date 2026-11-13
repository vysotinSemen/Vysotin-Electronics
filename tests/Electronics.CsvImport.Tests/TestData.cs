using Electronics.CsvImport.Services;

namespace Electronics.CsvImport.Tests;

internal static class TestData
{
    public static readonly string[] Columns = ProductImportService.Header.Split(',');
    public static readonly string[] Values = ["EL-001", "Учебный ноутбук", "Описание", "45990.50", "RUB", "Компьютеры", "5", "", "true", "Demo", "24", "Demo-15"];

    public static string Row(params (string Column, string Value)[] changes)
    {
        var values = Values.ToArray();
        foreach (var (column, value) in changes) values[Array.IndexOf(Columns, column)] = value;
        return string.Join(",", values.Select(Escape));
    }

    public static string Escape(string text) => text.IndexOfAny([',', '"']) >= 0
        ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;

    public static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "testdata", name);

    public static string NewDirectory()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "test-tmp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static ProductImportService Service(InMemoryProductRepository? repository = null, string? root = null) =>
        new(repository ?? new(), new CsvFilePathResolver(root ?? AppContext.BaseDirectory));
}
