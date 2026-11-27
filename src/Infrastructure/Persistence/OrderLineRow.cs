namespace Infrastructure.Persistence;

public sealed class OrderLineRow
{
    public int Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int Position { get; set; }

    public OrderRow Order { get; set; } = null!;
}