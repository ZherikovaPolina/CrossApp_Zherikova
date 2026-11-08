
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed class JsonOrderLineImporter : IOrderLineImporter
{
    public async Task<IReadOnlyList<OrderLine>> LoadAsync(
        string path,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            useAsync: true);

        List<OrderLineDto> dtos =
            await JsonSerializer.DeserializeAsync<List<OrderLineDto>>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken: ct)
            ?? throw new JsonException(
                $"Файл {Path.GetFileName(path)} не містить списку рядків замовлення.");

        var result = new List<OrderLine>(dtos.Count);

        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                OrderLineDto dto = dtos[i];

                if (dto is null)
                    throw new ArgumentException(
                        "Рядок замовлення не може бути null");

                OrderLine line = OrderLine.Create(
                    dto.ProductId,
                    dto.Name,
                    dto.Price,
                    dto.Quantity);

                result.Add(line);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException(
                    $"Запис {i + 1}: {ex.Message}",
                    ex);
            }
        }

        return result.AsReadOnly();
    }
}
