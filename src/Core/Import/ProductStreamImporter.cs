using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Domain;

namespace Core.Import;


public static class ProductStreamImporter
{
    public static async IAsyncEnumerable<Product> LoadAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ct.ThrowIfCancellationRequested();

        await using FileStream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        await foreach (JsonElement element in
            JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
                stream,
                cancellationToken: ct))
        {
            ct.ThrowIfCancellationRequested();

            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException(
                    "Очікувався JSON-об'єкт товару.");
            }

            string id = element
                .GetProperty("id")
                .GetString()
                ?? throw new JsonException(
                    "ID товару відсутній.");

            string name = element
                .GetProperty("name")
                .GetString()
                ?? throw new JsonException(
                    "Назва товару відсутня.");

            int quantity = element
                .GetProperty("availableQuantity")
                .GetInt32();

            Product product = Product.Create(
                id,
                name,
                quantity);

            ct.ThrowIfCancellationRequested();

            yield return product;
        }
    }

    public static async Task<List<Product>> ImportAsync(
        string path,
        CancellationToken ct = default)
    {
        var products = new List<Product>();

        await foreach (Product product in LoadAsync(path, ct))
        {
            ct.ThrowIfCancellationRequested();

            products.Add(product);
        }

        return products;
    }
}
