namespace Electronics.CsvImport.Services;

public sealed class CsvFilePathResolver(string contentRootPath)
{
    private readonly string _contentRoot = Path.GetFullPath(contentRootPath);

    public string Resolve(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Укажите путь к CSV-файлу.");

        var path = input.Trim();
        if (path.Length >= 2 && path.StartsWith('"') && path.EndsWith('"'))
            path = path[1..^1];
        if (path.StartsWith(@"\\") || path.StartsWith("//") || path.Contains("://"))
            throw new ArgumentException("Укажите путь к локальному файлу, а не сетевой адрес.");

        var portablePath = path.Replace('\\', '/');
        if (portablePath.StartsWith("/csv/", StringComparison.OrdinalIgnoreCase))
            path = "wwwroot" + portablePath;
        else if (Path.IsPathFullyQualified(path))
            return Path.GetFullPath(path);
        else if (Path.IsPathRooted(path))
            throw new ArgumentException("Используйте /csv/имя.csv или полный локальный путь с буквой диска.");

        var resolved = Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar), _contentRoot);
        var relative = Path.GetRelativePath(_contentRoot, resolved);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar))
            throw new ArgumentException("Относительный путь должен находиться внутри папки приложения. Для другого файла укажите полный путь.");
        return resolved;
    }
}
