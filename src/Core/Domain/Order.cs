using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public bool IsConfirmed { get; private set; }

    public IReadOnlyList<OrderLine> Lines =>
        _lines.AsReadOnly();

    public decimal Total =>
        _lines.Sum(line => line.Price * line.Quantity);

    private Order(
        string id,
        string customerId)
    {
        Id = id;
        CustomerId = customerId;
        IsConfirmed = false;
    }

    public static Order Create(string customerId)
    {
        return CreateWithId(
            Guid.NewGuid().ToString(),
            customerId);
    }

    private static Order CreateWithId(
        string id,
        string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(
                "Ідентифікатор замовлення обов'язковий",
                nameof(id));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException(
                "Ідентифікатор клієнта обов'язковий",
                nameof(customerId));

        return new Order(
            id.Trim(),
            customerId.Trim());
    }

    public void AddLine(
        string productId,
        string name,
        decimal price,
        int quantity)
    {
        if (IsConfirmed)
            throw new InvalidOperationException(
                $"Замовлення {Id} вже підтверджене, рядки додавати не можна");

        _lines.Add(OrderLine.Create(
            productId,
            name,
            price,
            quantity));
    }

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException(
                $"Замовлення {Id} не можна підтвердити, оскільки воно не містить товарів");

        IsConfirmed = true;
    }

    public OrderDto ToDto()
    {
        IReadOnlyList<OrderLineDto> lines = _lines
            .Select(line => new OrderLineDto(
                line.ProductId,
                line.Name,
                line.Price,
                line.Quantity))
            .ToList()
            .AsReadOnly();

        return new OrderDto(
            Id,
            CustomerId,
            IsConfirmed,
            lines);
    }

    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Order order = CreateWithId(
            dto.Id,
            dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
        {
            order.AddLine(
                line.ProductId,
                line.Name,
                line.Price,
                line.Quantity);
        }

        if (dto.IsConfirmed)
            order.Confirm();

        return order;
    }
}