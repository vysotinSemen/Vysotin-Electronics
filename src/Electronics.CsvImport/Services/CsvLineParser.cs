using System.Text;

namespace Electronics.CsvImport.Services;

public static class CsvLineParser
{
    public static IReadOnlyList<string> Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var fields = new List<string>();
        var value = new StringBuilder();
        var insideQuotes = false;
        var quoteClosed = false;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];
            if (current is '\r' or '\n')
                throw new FormatException("Перенос строки внутри поля не поддерживается.");

            if (insideQuotes)
            {
                if (current != '"') value.Append(current);
                else if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = false;
                    quoteClosed = true;
                }
            }
            else if (current == ',')
            {
                fields.Add(value.ToString().Trim());
                value.Clear();
                quoteClosed = false;
            }
            else if (quoteClosed)
            {
                if (!char.IsWhiteSpace(current))
                    throw new FormatException("После закрывающей кавычки ожидается запятая или конец строки.");
            }
            else if (current == '"')
            {
                if (!string.IsNullOrWhiteSpace(value.ToString()))
                    throw new FormatException("Двойная кавычка допустима только в начале поля.");
                value.Clear();
                insideQuotes = true;
            }
            else value.Append(current);
        }

        if (insideQuotes)
            throw new FormatException("Незакрытая двойная кавычка. Переносы строк внутри поля не поддерживаются.");
        fields.Add(value.ToString().Trim());
        return fields;
    }
}
