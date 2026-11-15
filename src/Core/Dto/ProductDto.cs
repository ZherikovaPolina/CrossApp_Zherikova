
namespace Core.Dto;

public record ProductDto(
    string Id,
    string Name,
    decimal Price)
{
    public string Sku { get; init; } = "";

    public string Unit { get; init; } = "шт";

    public int Quantity { get; init; } = 0;
}
