using Electronics.CsvImport.Services;

namespace Electronics.CsvImport.Tests;

public sealed class PathResolverTests
{
    [Theory]
    [InlineData("Import/sample_products.csv")]
    [InlineData("./Import/sample_products.csv")]
    [InlineData("Import\\sample_products.csv")]
    public void RelativePaths_ResolveFromApplicationRootAndImport(string path)
    {
        var root = TestData.NewDirectory();
        Directory.CreateDirectory(Path.Combine(root, "Import"));
        var expected = Path.Combine(root, "Import", "sample_products.csv");
        File.Copy(TestData.Fixture("sample_products.csv"), expected);
        Assert.Equal(expected, new CsvFilePathResolver(root).Resolve(path));
        var result = TestData.Service(root: root).ImportFromCsv(path);
        Assert.Equal(11, result.ImportedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FullLocalPathWithQuotes_IsSupported()
    {
        var path = TestData.Fixture("valid_electronics.csv");
        Assert.Equal(path, new CsvFilePathResolver(AppContext.BaseDirectory).Resolve(" \"" + path + "\" "));
        Assert.Equal(2, TestData.Service().ImportFromCsv("\"" + path + "\"").ImportedCount);
    }

    [Theory]
    [InlineData("../outside.csv")][InlineData("/csv/../../../outside.csv")]
    [InlineData("https://example.com/file.csv")][InlineData("\\\\server\\share\\file.csv")]
    public void NetworkOrEscapingRelativePath_IsRejected(string input) =>
        Assert.Throws<ArgumentException>(() => new CsvFilePathResolver(AppContext.BaseDirectory).Resolve(input));
}
