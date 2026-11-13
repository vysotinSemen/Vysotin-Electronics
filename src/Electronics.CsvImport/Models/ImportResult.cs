namespace Electronics.CsvImport.Models;

public sealed class ImportResult
{
    public int TotalRows { get; internal set; }
    public int ImportedCount { get; internal set; }
    public List<ImportError> Errors { get; } = [];
    public int ErrorCount => Errors.Count;
}

public sealed record ImportError(int RowNumber, string Message);
