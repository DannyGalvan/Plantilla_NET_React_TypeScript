using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using Project.Application.Crud;

namespace Project.Web.Crud;

public enum TabularFormat
{
    Csv,
    Xlsx,
}

public static class TabularFormats
{
    public static TabularFormat? FromName(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "csv" => TabularFormat.Csv,
        "xlsx" or "excel" => TabularFormat.Xlsx,
        _ => null,
    };

    public static TabularFormat? FromFileName(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".csv" or ".txt" => TabularFormat.Csv,
        ".xlsx" => TabularFormat.Xlsx,
        _ => null,
    };

    public static string ContentType(TabularFormat format) =>
        format == TabularFormat.Csv ? "text/csv; charset=utf-8" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static string Extension(TabularFormat format) => format == TabularFormat.Csv ? "csv" : "xlsx";
}

/// <summary>Writes a table as CSV (UTF-8 with BOM, opens correctly in Excel) or XLSX.</summary>
public static class TabularWriter
{
    public static byte[] Write(TabularFormat format, IReadOnlyList<string> headers, IEnumerable<object?[]> rows) =>
        format == TabularFormat.Csv ? Csv(headers, rows) : Xlsx(headers, rows);

    private static byte[] Csv(IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
    {
        var text = new StringBuilder();
        text.AppendJoin(',', headers.Select(Quote)).Append("\r\n");
        foreach (var row in rows)
        {
            text.AppendJoin(',', row.Select(value => Quote(Neutralize(Format(value))))).Append("\r\n");
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private static byte[] Xlsx(IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data");
        for (var c = 0; c < headers.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                // Text cells are never evaluated as formulas, so no neutralization is needed here.
                sheet.Cell(r, c + 1).Value = row[c] switch
                {
                    null => Blank.Value,
                    bool b => b,
                    int i => i,
                    long l => l,
                    decimal d => d,
                    double d => d,
                    DateTimeOffset date => date.UtcDateTime,
                    DateTime date => date,
                    var other => Format(other),
                };
            }

            r++;
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents(1, Math.Min(r, 200));
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>CSV injection: a cell starting with = + - @ would run as a formula when the file is opened.</summary>
    private static string Neutralize(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
            ? "'" + value
            : value;

    private static string Quote(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n', ';']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}

/// <summary>Reads the first sheet of an XLSX or a CSV (comma or semicolon) into header and rows of text.</summary>
public static class TabularReader
{
    public static (IReadOnlyList<string> Headers, IReadOnlyList<string?[]> Rows) Read(Stream content, TabularFormat format, int maxRows)
    {
        var table = format == TabularFormat.Csv ? Csv(content, maxRows) : Xlsx(content, maxRows);
        if (table.Count == 0)
        {
            return ([], []);
        }

        var headers = table[0].Select(h => h?.Trim() ?? string.Empty).ToList();
        var rows = table.Skip(1).Where(r => r.Any(v => !string.IsNullOrWhiteSpace(v))).ToList();
        return (headers, rows);
    }

    private static List<string?[]> Xlsx(Stream content, int maxRows)
    {
        using var workbook = new XLWorkbook(content);
        var sheet = workbook.Worksheets.First();
        var used = sheet.RangeUsed();
        if (used is null)
        {
            return [];
        }

        var columns = used.ColumnCount();
        return sheet.RowsUsed().Take(maxRows + 1).Select(row => Enumerable.Range(1, columns).Select(c => CellText(row.Cell(c))).ToArray()).ToList();
    }

    private static string? CellText(IXLCell cell) => cell.Value.Type switch
    {
        XLDataType.Blank => null,
        XLDataType.Number => cell.Value.GetNumber().ToString(CultureInfo.InvariantCulture),
        XLDataType.Boolean => cell.Value.GetBoolean() ? "true" : "false",
        XLDataType.DateTime => cell.Value.GetDateTime().ToString("O", CultureInfo.InvariantCulture),
        _ => cell.GetFormattedString(),
    };

    private static List<string?[]> Csv(Stream content, int maxRows)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var firstLine = text.AsSpan(0, Math.Max(0, text.IndexOfAny(['\r', '\n']) is var end and >= 0 ? end : text.Length));
        var delimiter = firstLine.Count(';') > firstLine.Count(',') ? ';' : ',';

        var rows = new List<string?[]>();
        var row = new List<string?>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length && rows.Count <= maxRows; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(field.ToString());
                field.Clear();
                rows.Add([.. row]);
                row.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add([.. row]);
        }

        return rows;
    }
}

/// <summary>Scalar public properties of a DTO, in declaration order (the exported columns).</summary>
public static class ExportColumns<T>
{
    private static readonly HashSet<string> Hidden = ["ConcurrencyStamp"];

    public static IReadOnlyList<PropertyInfo> Properties { get; } = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && ImportBinder.IsScalar(p.PropertyType) && !Hidden.Contains(p.Name))
        .ToList();

    public static IReadOnlyList<string> Headers { get; } = Properties.Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToList();

    public static object?[] Values(T item) => Properties.Select(p => p.GetValue(item)).ToArray();
}

/// <summary>
/// Turns the text cells of an import file into the create request of a resource: columns are matched to properties
/// by name (case insensitive), values are converted to the property type (numbers with dot or comma, booleans as
/// true/false/1/0/si/no) and conversion problems are reported per row.
/// </summary>
public static class ImportBinder
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static readonly ConcurrentDictionary<Type, Dictionary<string, Type>> Targets = new();
    private static readonly string[] TrueWords = ["true", "1", "si", "sí", "yes", "x"];
    private static readonly string[] FalseWords = ["false", "0", "no"];

    public static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid)
            || t == typeof(DateTimeOffset) || t == typeof(DateTime) || t == typeof(DateOnly) || t == typeof(TimeOnly);
    }

    public static IReadOnlyList<ImportRow<T>> Bind<T>(IReadOnlyList<string> headers, IReadOnlyList<string?[]> rows)
        where T : notnull
    {
        var targets = Targets.GetOrAdd(typeof(T), static type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsScalar(p.PropertyType))
            .ToDictionary(p => p.Name, p => p.PropertyType, StringComparer.OrdinalIgnoreCase));

        var columns = headers.Select(h => targets.TryGetValue(h.Replace(" ", string.Empty, StringComparison.Ordinal), out var type)
            ? (Name: targets.Keys.First(k => k.Equals(h.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)), Type: type)
            : (Name: (string?)null, Type: (Type?)null)).ToList();

        var result = new List<ImportRow<T>>(rows.Count);
        for (var r = 0; r < rows.Count; r++)
        {
            var errors = new List<string>();
            var json = new JsonObject();
            for (var c = 0; c < columns.Count && c < rows[r].Length; c++)
            {
                var (name, type) = columns[c];
                var raw = rows[r][c]?.Trim();
                if (name is null || type is null || string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                if (Convert(raw, type) is { } node)
                {
                    json[JsonNamingPolicy.CamelCase.ConvertName(name)] = node;
                }
                else
                {
                    errors.Add($"{headers[c]}: '{raw}' is not a valid value.");
                }
            }

            T? value = default;
            if (errors.Count == 0)
            {
                try
                {
                    value = json.Deserialize<T>(Json);
                }
                catch (JsonException ex)
                {
                    errors.Add(ex.Message);
                }
            }

            // Row numbers as the user sees them in the file (row 1 is the header).
            result.Add(new ImportRow<T>(r + 2, value, errors));
        }

        return result;
    }

    private static JsonNode? Convert(string raw, Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t == typeof(string) || t.IsEnum)
        {
            return JsonValue.Create(raw);
        }

        if (t == typeof(bool))
        {
            var lower = raw.ToLowerInvariant();
            return TrueWords.Contains(lower) ? JsonValue.Create(true) : FalseWords.Contains(lower) ? JsonValue.Create(false) : null;
        }

        if (t == typeof(Guid))
        {
            return Guid.TryParse(raw, out var id) ? JsonValue.Create(id) : null;
        }

        if (t == typeof(DateTimeOffset) || t == typeof(DateTime) || t == typeof(DateOnly))
        {
            return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
                ? JsonValue.Create(t == typeof(DateOnly) ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : date.ToString("O", CultureInfo.InvariantCulture))
                : null;
        }

        if (!decimal.TryParse(NormalizeNumber(raw), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
        {
            return number == decimal.Truncate(number) ? JsonValue.Create((long)number) : null;
        }

        return JsonValue.Create(number);
    }

    /// <summary>
    /// "1234.5", "1,234.5" (English) and "1234,5", "1.234,5" (Spanish): when both separators appear, the last one is
    /// the decimal separator; a lone comma is a decimal comma.
    /// </summary>
    private static string NormalizeNumber(string raw)
    {
        var comma = raw.LastIndexOf(',');
        var dot = raw.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
        {
            return comma > dot
                ? raw.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
                : raw.Replace(",", string.Empty, StringComparison.Ordinal);
        }

        return comma >= 0 ? raw.Replace(',', '.') : raw;
    }
}
