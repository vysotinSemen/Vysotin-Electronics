using System.Globalization;
using System.Text;
using System.Text.Json;
using Electronics.CsvImport.Services;

namespace Electronics.CsvImport.Tests;

public sealed class ProductImportServiceTests
{
    private const string Header = ProductImportService.Header;

    public static IEnumerable<object[]> FixtureCases()
    {
        var json = File.ReadAllText(TestData.Fixture("fixture_expectations.json"));
        foreach (var (name, values) in JsonSerializer.Deserialize<Dictionary<string, int[]>>(json)!)
            yield return new object[] { name, values[0], values[1], values[2] };
    }

    [Theory]
    [MemberData(nameof(FixtureCases))]
    public void Fixture_ProducesExpectedCounters(string file, int rows, int imported, int errors)
    {
        var repository = new InMemoryProductRepository();
        var result = TestData.Service(repository).ImportFromCsv(TestData.Fixture(file));
        Assert.Equal(rows, result.TotalRows);
        Assert.Equal(imported, result.ImportedCount);
        Assert.Equal(errors, result.ErrorCount);
        Assert.Equal(imported, repository.Count);
    }

    [Fact]
    public void ValidRow_MapsAllTwelveFields()
    {
        var repository = new InMemoryProductRepository();
        var result = TestData.Service(repository).ImportFromLines([Header, TestData.Row(("image_url", "/demo.jpg"))]);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);
        var item = Assert.Single(repository.GetAll());
        Assert.Equal("EL-001", item.Sku);
        Assert.Equal("Учебный ноутбук", item.Name);
        Assert.Equal("Описание", item.Description);
        Assert.Equal(45990.50m, item.Price);
        Assert.Equal("RUB", item.Currency);
        Assert.Equal("Компьютеры", item.CategoryName);
        Assert.Equal(5, item.StockQuantity);
        Assert.Equal("/demo.jpg", item.ImageUrl);
        Assert.True(item.IsActive);
        Assert.Equal("Demo", item.Brand);
        Assert.Equal(24, item.WarrantyMonths);
        Assert.Equal("Demo-15", item.ModelNumber);
    }

    [Theory]
    [InlineData("sku", "")][InlineData("name", " ")]
    [InlineData("price", "0")][InlineData("price", "-1")][InlineData("price", "abc")]
    [InlineData("price", "1,5")][InlineData("price", "1e3")][InlineData("price", "1 000")]
    [InlineData("price", "999999999999999999999999999999999")]
    [InlineData("stock", "-1")][InlineData("stock", "1.5")][InlineData("stock", "abc")]
    [InlineData("stock", "9999999999999999")]
    [InlineData("is_active", "yes")][InlineData("is_active", "")]
    [InlineData("brand", " ")]
    [InlineData("warranty_months", "-1")][InlineData("warranty_months", "61")]
    [InlineData("warranty_months", "1.5")][InlineData("warranty_months", "abc")]
    [InlineData("warranty_months", "")][InlineData("warranty_months", "9999999999999999")]
    public void InvalidField_RejectsRowWithPhysicalNumber(string field, string value)
    {
        var repository = new InMemoryProductRepository();
        var result = TestData.Service(repository).ImportFromLines([Header, TestData.Row((field, value))]);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        var error = Assert.Single(result.Errors);
        Assert.Equal(2, error.RowNumber);
        Assert.Contains(field, error.Message);
        Assert.Empty(repository.GetAll());
    }

    [Theory]
    [InlineData("sku")][InlineData("name")][InlineData("description")][InlineData("price")]
    [InlineData("currency")][InlineData("category_name")][InlineData("stock")][InlineData("image_url")]
    [InlineData("is_active")][InlineData("brand")][InlineData("warranty_months")][InlineData("model_number")]
    public void MissingAnyColumn_IsHeaderError(string column)
    {
        var header = string.Join(",", TestData.Columns.Where(c => c != column));
        var result = TestData.Service().ImportFromLines([header]);
        Assert.Equal(0, result.TotalRows);
        var error = Assert.Single(result.Errors);
        Assert.Equal(1, error.RowNumber);
        Assert.Contains(column, error.Message);
    }

    [Theory]
    [InlineData("true", true)][InlineData("FALSE", false)][InlineData("1", true)][InlineData("0", false)]
    public void AllowedActivityValues_AreMapped(string text, bool expected)
    {
        var repository = new InMemoryProductRepository();
        Assert.Empty(TestData.Service(repository).ImportFromLines([Header, TestData.Row(("is_active", text))]).Errors);
        Assert.Equal(expected, Assert.Single(repository.GetAll()).IsActive);
    }

    [Theory]
    [InlineData("0", 0)][InlineData("60", 60)][InlineData("24", 24)]
    public void WarrantyBounds_AreInclusive(string text, int expected)
    {
        var repository = new InMemoryProductRepository();
        Assert.Empty(TestData.Service(repository).ImportFromLines([Header, TestData.Row(("warranty_months", text))]).Errors);
        Assert.Equal(expected, Assert.Single(repository.GetAll()).WarrantyMonths);
    }

    [Fact]
    public void OptionalFields_DefaultsAndNormalization_AreApplied()
    {
        var repository = new InMemoryProductRepository();
        var result = TestData.Service(repository).ImportFromLines([Header, TestData.Row(("description", ""), ("image_url", ""), ("model_number", ""), ("currency", " "), ("category_name", ""), ("stock", "0"), ("brand", " Demo "))]);
        Assert.Empty(result.Errors);
        var product = Assert.Single(repository.GetAll());
        Assert.Equal("", product.Description);
        Assert.Equal("", product.ImageUrl);
        Assert.Equal("", product.ModelNumber);
        Assert.Equal("RUB", product.Currency);
        Assert.Equal("Без категории", product.CategoryName);
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal("Demo", product.Brand);
    }

    [Fact]
    public void AllFieldErrors_AreReportedAndNextRowIsImported()
    {
        var result = TestData.Service().ImportFromLines([Header, TestData.Row(("sku", ""), ("name", ""), ("price", "0"), ("stock", "-1"), ("is_active", "bad"), ("brand", ""), ("warranty_months", "61")), TestData.Row()]);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(7, result.ErrorCount);
        Assert.All(result.Errors, error => Assert.Equal(2, error.RowNumber));
    }

    [Fact]
    public void DuplicateWithinFile_PreservesFirstValue()
    {
        var repository = new InMemoryProductRepository();
        var result = TestData.Service(repository).ImportFromLines([Header, TestData.Row(), TestData.Row(("sku", " el-001 "), ("name", "Второй"))]);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(3, Assert.Single(result.Errors).RowNumber);
        Assert.Equal("Учебный ноутбук", Assert.Single(repository.GetAll()).Name);
    }

    [Fact]
    public void RepeatedMainFile_SkipsAllElevenExistingSkus()
    {
        var repository = new InMemoryProductRepository();
        var service = TestData.Service(repository);
        Assert.Equal(11, service.ImportFromCsv(TestData.Fixture("sample_products.csv")).ImportedCount);
        var second = service.ImportFromCsv(TestData.Fixture("sample_products.csv"));
        Assert.Equal(11, second.TotalRows);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(11, second.ErrorCount);
        Assert.Equal(11, repository.Count);
    }

    [Fact]
    public void ParallelImports_CannotDuplicateSku()
    {
        var repository = new InMemoryProductRepository();
        var service = TestData.Service(repository);
        var results = Enumerable.Range(0, 40).AsParallel().Select(_ => service.ImportFromLines([Header, TestData.Row()])).ToArray();
        Assert.Equal(1, results.Sum(r => r.ImportedCount));
        Assert.Equal(39, results.Sum(r => r.ErrorCount));
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void BlankLines_DoNotChangePhysicalErrorNumber()
    {
        var result = TestData.Service().ImportFromLines(["", " ", Header, "", TestData.Row(("price", "0")), " ", TestData.Row()]);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(5, Assert.Single(result.Errors).RowNumber);
    }

    [Fact]
    public void ReorderedCaseInsensitiveColumns_IgnoreExtraColumn()
    {
        var header = string.Join(",", TestData.Columns.Reverse().Select(c => " " + c.ToUpperInvariant() + " ").Append("extra"));
        var row = string.Join(",", TestData.Values.Reverse().Select(TestData.Escape).Append("ignored"));
        var repository = new InMemoryProductRepository();
        Assert.Empty(TestData.Service(repository).ImportFromLines([header, row]).Errors);
        Assert.Equal(24, Assert.Single(repository.GetAll()).WarrantyMonths);
    }

    [Fact]
    public void DuplicateHeader_IsRejected()
    {
        var result = TestData.Service().ImportFromLines([Header + ", SKU"]);
        Assert.Equal(0, result.TotalRows);
        Assert.Contains("повторяется", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void BadStructure_DoesNotPreventNextValidRow()
    {
        var result = TestData.Service().ImportFromLines([Header, "one,two", "\"Незакрыто", TestData.Row()]);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(new[] { 2, 3 }, result.Errors.Select(e => e.RowNumber));
    }

    [Fact]
    public void QuotedCommaEscapedQuotesAndCyrillic_ArePreserved()
    {
        var repository = new InMemoryProductRepository();
        Assert.Empty(TestData.Service(repository).ImportFromLines([Header, TestData.Row(("name", "Товар, учебный"), ("description", "Вариант \"Учёба\""), ("currency", " rub "))]).Errors);
        var product = Assert.Single(repository.GetAll());
        Assert.Equal("Товар, учебный", product.Name);
        Assert.Equal("Вариант \"Учёба\"", product.Description);
        Assert.Equal("RUB", product.Currency);
    }

    [Theory]
    [InlineData("ru-RU")][InlineData("en-US")][InlineData("tr-TR")]
    public void DecimalPoint_DoesNotDependOnCulture(string culture)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var repository = new InMemoryProductRepository();
            Assert.Empty(TestData.Service(repository).ImportFromLines([Header, TestData.Row()]).Errors);
            Assert.Equal(45990.50m, Assert.Single(repository.GetAll()).Price);
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public void Utf8WithOrWithoutBom_ImportsCyrillic(bool bom)
    {
        var path = Path.Combine(TestData.NewDirectory(), "электроника с пробелами.csv");
        File.WriteAllText(path, Header + "\n" + TestData.Row(), new UTF8Encoding(bom));
        var repository = new InMemoryProductRepository();
        Assert.Empty(TestData.Service(repository).ImportFromCsv(path).Errors);
        Assert.Equal("Учебный ноутбук", Assert.Single(repository.GetAll()).Name);
    }

    [Fact]
    public void MissingFile_ReturnsFileLevelError()
    {
        var result = TestData.Service().ImportFromCsv(Path.Combine(TestData.NewDirectory(), "missing.csv"));
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, Assert.Single(result.Errors).RowNumber);
        Assert.Contains("не найден", result.Errors[0].Message);
    }

    [Fact]
    public void LockedFile_ReturnsErrorWithoutThrowing()
    {
        var path = Path.Combine(TestData.NewDirectory(), "locked.csv");
        using var locked = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Assert.Contains("занят", Assert.Single(TestData.Service().ImportFromCsv(path).Errors).Message);
    }

    [Fact]
    public void DirectoryInsteadOfFile_ReturnsAccessError() =>
        Assert.Contains("недоступен", Assert.Single(TestData.Service().ImportFromCsv(TestData.NewDirectory()).Errors).Message);

    [Fact]
    public void NonUtf8Bytes_ReturnEncodingError()
    {
        var path = Path.Combine(TestData.NewDirectory(), "encoding.csv");
        File.WriteAllBytes(path, [0xff, 0xfe, 0xff]);
        Assert.Contains("UTF-8", Assert.Single(TestData.Service().ImportFromCsv(path).Errors).Message);
    }

    [Theory]
    [InlineData(null)][InlineData("")][InlineData(" ")][InlineData("bad\0path")][InlineData("https://example.com/a.csv")]
    public void InvalidPath_ReturnsError(string? path)
    {
        var result = TestData.Service().ImportFromCsv(path);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void EmptyLinesAndBomBeforeHeader_AreHandled()
    {
        Assert.Contains("отсутствуют", Assert.Single(TestData.Service().ImportFromLines(["", " "]).Errors).Message);
        var result = TestData.Service().ImportFromLines(["\uFEFF" + Header, TestData.Row()]);
        Assert.Empty(result.Errors);
        Assert.Equal(1, result.ImportedCount);
    }
}
