using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("JSON повинен містити масив товарів");
                return new ImportResult<ProductDto>(items, errors);
            }

            int number = 0;

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                number++;

                if (element.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"елемент {number}: очікувався об'єкт");
                    continue;
                }

                if (!element.TryGetProperty("id", out JsonElement idElement) ||
                    idElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(idElement.GetString()))
                {
                    errors.Add($"елемент {number}: ID відсутній або має неправильний тип");
                    continue;
                }

                if (!element.TryGetProperty("name", out JsonElement nameElement) ||
                    nameElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(nameElement.GetString()))
                {
                    errors.Add($"елемент {number}: назва відсутня або має неправильний тип");
                    continue;
                }

                if (!element.TryGetProperty("price", out JsonElement priceElement) ||
                    priceElement.ValueKind != JsonValueKind.Number ||
                    !priceElement.TryGetDecimal(out decimal price))
                {
                    errors.Add($"елемент {number}: ціна відсутня або має неправильний тип");
                    continue;
                }

                items.Add(new ProductDto(
                    idElement.GetString()!,
                    nameElement.GetString()!,
                    price));
            }
        }
        catch (JsonException)
        {
            errors.Add("помилка формату JSON");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}