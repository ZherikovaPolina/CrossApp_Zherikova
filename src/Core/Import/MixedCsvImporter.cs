using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    public static ImportResult<object> Load(string path)
    {
        var results = new List<object>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(
                ';',
                StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                errors.Add($"рядок {number}: порожній рядок");
                continue;
            }

            switch (parts[0])
            {
                case "P":
                    if (parts.Length != 4)
                    {
                        errors.Add(
                            $"рядок {number}: для Product потрібно 4 колонки");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(parts[1]) ||
                        string.IsNullOrWhiteSpace(parts[2]))
                    {
                        errors.Add(
                            $"рядок {number}: ID або назва товару порожні");
                        continue;
                    }

                    if (!decimal.TryParse(
                            parts[3],
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out decimal price))
                    {
                        errors.Add(
                            $"рядок {number}: ціна '{parts[3]}' не є коректним числом");
                        continue;
                    }

                    if (price <= 0)
                    {
                        errors.Add(
                            $"рядок {number}: ціна повинна бути більшою за 0");
                        continue;
                    }

                    results.Add(new ProductDto(
                        parts[1],
                        parts[2],
                        price));

                    break;

                case "O":
    if (parts.Length != 7)
    {
        errors.Add(
            $"рядок {number}: для Order потрібно 7 колонок");
        continue;
    }

    if (string.IsNullOrWhiteSpace(parts[1]) ||
        string.IsNullOrWhiteSpace(parts[2]) ||
        string.IsNullOrWhiteSpace(parts[3]) ||
        string.IsNullOrWhiteSpace(parts[4]))
    {
        errors.Add(
            $"рядок {number}: дані замовлення не можуть бути порожніми");
        continue;
    }

    if (!decimal.TryParse(
            parts[5],
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal orderPrice))
    {
        errors.Add(
            $"рядок {number}: ціна '{parts[5]}' не є коректним числом");
        continue;
    }

    if (orderPrice < 0)
    {
        errors.Add(
            $"рядок {number}: ціна не може бути від'ємною");
        continue;
    }

    if (!int.TryParse(parts[6], out int quantity))
    {
        errors.Add(
            $"рядок {number}: кількість '{parts[6]}' не є цілим числом");
        continue;
    }

    if (quantity <= 0)
    {
        errors.Add(
            $"рядок {number}: кількість повинна бути більшою за 0");
        continue;
    }

    OrderLineDto orderLine = new(
        parts[3],
        parts[4],
        orderPrice,
        quantity);

    results.Add(new OrderDto(
        parts[1],
        parts[2],
        false,
        new List<OrderLineDto> { orderLine }));

    break;

                default:
                    errors.Add(
                        $"рядок {number}: невідомий тип рядка '{parts[0]}'");
                    break;
            }
        }

        return new ImportResult<object>(results, errors);
    }
}