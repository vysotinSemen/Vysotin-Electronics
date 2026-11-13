using Electronics.CsvImport.Services;
namespace Electronics.CsvImport.Tests;

public sealed class CsvLineParserTests
{
    [Fact]
    public void SimpleFields_AreTrimmed() => Assert.Equal(new[] { "a", "дом", "3" }, CsvLineParser.Parse(" a , дом ,3"));
    [Fact]
    public void EmptyFields_IncludingTrailing_AreRetained() => Assert.Equal(new[] { "a", "", "", "" }, CsvLineParser.Parse("a,,,"));
    [Fact]
    public void QuotedComma_IsPartOfField() => Assert.Equal(new[] { "a", "Дом, учебный", "b" }, CsvLineParser.Parse("a,\"Дом, учебный\",b"));
    [Fact]
    public void EscapedQuotes_AreUnescaped() => Assert.Equal(new[] { "Дом \"Комфорт\"" }, CsvLineParser.Parse("\"Дом \"\"Комфорт\"\"\""));
    [Fact]
    public void QuotedEmptyAndExternalSpaces_AreSupported() => Assert.Equal(new[] { "", "дом" }, CsvLineParser.Parse(" \"\" , \" дом \" "));

    [Theory]
    [InlineData("a,\"незакрытая")]
    [InlineData("ab\"cd,b")]
    [InlineData("\"a\"tail,b")]
    [InlineData("\"a\"\"")]
    [InlineData("\"много\nстрок\"")]
    public void MalformedQuotes_ProduceFormatError(string line) => Assert.Throws<FormatException>(() => CsvLineParser.Parse(line));
}
