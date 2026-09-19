using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    public static List<object> Load(string path)
    {
        string[] lines = File.ReadAllLines(path);

        var results = new List<object>();

        foreach (string line in lines)
        {
            string[] parts = line.Split(';', StringSplitOptions.TrimEntries);

            object result = parts[0] switch
            {
                "P" => new ProductDto(
                    parts[1],
                    parts[2],
                    decimal.Parse(
                        parts[3],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture)),

                "O" => new OrderDto(
                    parts[1],
                    parts[2],
                    parts[3],
                    int.Parse(parts[4])),

                _ => throw new FormatException(
                    $"Невідомий тип рядка: {parts[0]}")
            };

            results.Add(result);
        }

        return results;
    }
}