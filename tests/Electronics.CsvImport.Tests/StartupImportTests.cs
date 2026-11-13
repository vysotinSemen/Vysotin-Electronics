using Electronics.CsvImport.Models;
using Electronics.CsvImport.Services;
using Microsoft.Extensions.Configuration;

namespace Electronics.CsvImport.Tests;

public sealed class StartupImportTests
{
    [Fact]
    public void ShippedConfiguration_LoadsElevenBeforeAnyRequest_AndRunsOnce()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile(TestData.Fixture("appsettings.json")).Build();
        var options = configuration.GetSection("CsvImport").Get<CsvImportOptions>()!;
        var root = TestData.NewDirectory();
        Directory.CreateDirectory(Path.Combine(root, options.DirectoryPath));
        File.Copy(TestData.Fixture(options.FileName), Path.Combine(root, options.DirectoryPath, options.FileName));
        var repository = new InMemoryProductRepository();
        var startup = new StartupImportService(TestData.Service(repository, root), options);
        var report = startup.Run();
        Assert.Equal(11, report.Result.TotalRows);
        Assert.Equal(11, report.Result.ImportedCount);
        Assert.Empty(report.Result.Errors);
        Assert.Equal(11, repository.Count);
        Assert.Same(report, startup.Run());
        Assert.Equal(11, repository.Count);
    }

    [Theory]
    [InlineData("missing.csv")][InlineData("empty_file.csv")][InlineData("invalid_header.csv")]
    [InlineData("missing_brand.csv")][InlineData("malformed_quotes.csv")]
    public void BadStartupFile_ReturnsVisibleError_AndAllowsManualRecovery(string filename)
    {
        var repository = new InMemoryProductRepository();
        var service = TestData.Service(repository);
        var startup = new StartupImportService(service, new()
        {
            DirectoryPath = Path.GetDirectoryName(TestData.Fixture(filename))!, FileName = filename
        });
        Assert.NotEmpty(startup.Run().Result.Errors);
        Assert.Empty(repository.GetAll());
        Assert.Equal(11, service.ImportFromCsv(TestData.Fixture("sample_products.csv")).ImportedCount);
        Assert.Equal(11, repository.Count);
    }

    [Fact]
    public void EmptyConfiguredFilename_IsConfigurationError()
    {
        var startup = new StartupImportService(TestData.Service(), new() { FileName = " " });
        var error = Assert.Single(startup.Run().Result.Errors);
        Assert.Equal(0, error.RowNumber);
        Assert.Contains("CsvImport:FileName", error.Message);
    }

    [Fact]
    public void InvalidConfiguredPath_ReturnsErrorInsteadOfCrashing()
    {
        var startup = new StartupImportService(TestData.Service(), new() { DirectoryPath = "bad\0path" });
        Assert.Single(startup.Run().Result.Errors);
    }
}
