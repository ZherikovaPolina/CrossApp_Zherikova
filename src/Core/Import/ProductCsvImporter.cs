using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
{
    string[] parts = line.Split(
        Separator,
        StringSplitOptions.TrimEntries);

    return parts switch
    {
        { Length: < 3 }
            => new ParseFailed(
                $"очікую 3 колонки, отримав {parts.Length}"),

        ["", _, _] or [_, "", _]
        => new ParseFailed(
        "ID або назва порожні"),

        [var id, var name, var price]
        when !decimal.TryParse(
            price,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out _)
        => new ParseFailed(
            $"ціна '{price}' не є коректним числом"),

    [var id, var name, var price]
        when decimal.TryParse(
            price,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal parsedPrice) && parsedPrice <= 0
        => new ParseFailed(
            $"ціна '{price}' повинна бути більшою за 0"),

        [var id, var name, var price]
            => new ParseOk(
                new ProductDto(
                    id,
                    name,
                    decimal.Parse(
                        price,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture))),

        _ => new ParseFailed(
            $"занадто багато колонок: {parts.Length}")
    };
}

    private abstract record ParseOutcome;

    private sealed record ParseOk(ProductDto Value) : ParseOutcome;

    private sealed record ParseFailed(string Reason) : ParseOutcome;
}