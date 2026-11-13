using System.Globalization;
using System.Text;
using Electronics.CsvImport.Models;

namespace Electronics.CsvImport.Services;

public sealed class ProductImportService(
    IProductRepository repository,
    CsvFilePathResolver pathResolver) : IProductImportService
{
    public const string Header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";
    private const string DefaultCurrency = "RUB";
    private const string DefaultCategory = "Без категории";
    private const int MaximumWarrantyMonths = 60;
    private static readonly string[] RequiredColumns = Header.Split(',');

    public ImportResult ImportFromCsv(string? filePath)
    {
        var result = new ImportResult();
        try
        {
            var path = pathResolver.Resolve(filePath);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), false);
            ProcessLines(ReadLines(reader), result);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            result.Errors.Add(new(0, "Файл не найден. Проверьте путь к CSV-файлу."));
        }
        catch (UnauthorizedAccessException)
        {
            result.Errors.Add(new(0, "Файл недоступен для чтения. Проверьте права доступа и укажите файл, а не папку."));
        }
        catch (DecoderFallbackException)
        {
            result.Errors.Add(new(0, "Неверная кодировка. Сохраните файл в UTF-8 с BOM или без него. Уже добавленные товары сохранены."));
        }
        catch (IOException)
        {
            result.Errors.Add(new(0, "Не удалось прочитать файл: он занят или произошла ошибка ввода-вывода. Уже добавленные товары сохранены."));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            result.Errors.Add(new(0, "Некорректный путь. " + ex.Message));
        }
        return result;
    }

    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var result = new ImportResult();
        ProcessLines(lines, result);
        return result;
    }

    private static IEnumerable<string> ReadLines(TextReader reader)
    {
        while (reader.ReadLine() is { } line) yield return line;
    }

    private void ProcessLines(IEnumerable<string> lines, ImportResult result)
    {
        Dictionary<string, int>? columns = null;
        var fieldCount = 0;
        var rowNumber = 0;

        foreach (var originalLine in lines)
        {
            rowNumber++;
            var line = rowNumber == 1 ? originalLine.TrimStart('\uFEFF') : originalLine;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (columns is not null) result.TotalRows++;

            IReadOnlyList<string> fields;
            try { fields = CsvLineParser.Parse(line); }
            catch (FormatException ex)
            {
                result.Errors.Add(new(rowNumber, "Ошибка CSV: " + ex.Message));
                if (columns is null) return;
                continue;
            }

            if (columns is null)
            {
                columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < fields.Count; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (!columns.TryAdd(fields[i], i))
                    {
                        result.Errors.Add(new(rowNumber, $"Неверные заголовки CSV: колонка {fields[i]} повторяется."));
                        return;
                    }
                }
                var missing = RequiredColumns.Where(c => !columns.ContainsKey(c)).ToArray();
                if (missing.Length > 0)
                {
                    result.Errors.Add(new(rowNumber, "Неверные заголовки CSV. Отсутствуют: " + string.Join(", ", missing)));
                    return;
                }
                fieldCount = fields.Count;
                continue;
            }

            if (fields.Count != fieldCount)
            {
                result.Errors.Add(new(rowNumber, $"Неверное количество полей: ожидалось {fieldCount}, получено {fields.Count}."));
                continue;
            }

            var product = ValidateAndMap(fields, columns, rowNumber, result);
            if (product is null) continue;
            if (repository.TryAdd(product)) result.ImportedCount++;
            else result.Errors.Add(new(rowNumber, $"Артикул {product.Sku} уже существует. Дубликат пропущен."));
        }

        if (result.TotalRows == 0 && result.ErrorCount == 0)
            result.Errors.Add(new(0, "Данные для импорта отсутствуют. Файл пуст или содержит только заголовок."));
    }

    private static Product? ValidateAndMap(
        IReadOnlyList<string> fields, Dictionary<string, int> columns, int rowNumber, ImportResult result)
    {
        string Value(string column) => fields[columns[column]];
        void Error(string message) => result.Errors.Add(new(rowNumber, message));
        var errorsBefore = result.ErrorCount;

        var sku = Value("sku");
        var name = Value("name");
        if (sku.Length == 0) Error("Поле sku не может быть пустым.");
        if (name.Length == 0) Error("Поле name не может быть пустым.");
        if (!TryDecimal(Value("price"), out var price) || price <= 0)
            Error("Поле price должно быть числом больше 0. Дробный разделитель — точка.");
        if (!TryInt(Value("stock"), out var stock) || stock < 0)
            Error("Поле stock должно быть целым числом не меньше 0.");
        var activeText = Value("is_active");
        var active = activeText.Equals("true", StringComparison.OrdinalIgnoreCase) || activeText == "1";
        if (!active && !activeText.Equals("false", StringComparison.OrdinalIgnoreCase) && activeText != "0")
            Error("Поле is_active должно иметь значение true, false, 1 или 0.");
        var brand = Value("brand");
        if (brand.Length == 0)
            Error("Поле brand обязательно.");
        if (!TryInt(Value("warranty_months"), out var warranty) || warranty < 0 || warranty > MaximumWarrantyMonths)
            Error($"Поле warranty_months должно быть целым числом от 0 до {MaximumWarrantyMonths} включительно.");

        if (result.ErrorCount != errorsBefore) return null;
        return new Product
        {
            Sku = sku, Name = name, Description = Value("description"),
            Price = price, Currency = Value("currency").Length == 0 ? DefaultCurrency : Value("currency").ToUpperInvariant(),
            CategoryName = Value("category_name").Length == 0 ? DefaultCategory : Value("category_name"),
            StockQuantity = stock, ImageUrl = Value("image_url"), IsActive = active,
            Brand = brand, WarrantyMonths = warranty, ModelNumber = Value("model_number")
        };
    }

    private static bool TryDecimal(string text, out decimal value) => decimal.TryParse(
        text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);

    private static bool TryInt(string text, out int value) => int.TryParse(
        text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
}
