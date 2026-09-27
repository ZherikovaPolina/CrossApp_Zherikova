namespace Core.Domain;

public sealed class Product
{
    public string Id { get; }
    public string Name { get; }
    public int AvailableQuantity { get; private set; }

    private Product(
        string id,
        string name,
        int availableQuantity)
    {
        Id = id;
        Name = name;
        AvailableQuantity = availableQuantity;
    }

    public static Product Create(
        string id,
        string name,
        int availableQuantity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(
                "Ідентифікатор товару обов'язковий",
                nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Назва товару не може бути порожньою",
                nameof(name));

        if (availableQuantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(availableQuantity),
                availableQuantity,
                "Доступна кількість товару не може бути від'ємною");

        return new Product(
            id.Trim(),
            name.Trim(),
            availableQuantity);
    }
}