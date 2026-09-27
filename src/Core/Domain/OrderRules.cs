namespace Core.Domain;

public static class OrderRules
{
    public static void EnsureProductsAvailable(
        Order order,
        IReadOnlyList<Product> products)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(products);

        foreach (OrderLine line in order.Lines)
        {
            Product? product = products.FirstOrDefault(
                p => p.Id == line.ProductId);

            if (product is null)
                throw new InvalidOperationException(
                    $"Товар {line.ProductId} не знайдено");

            if (product.AvailableQuantity < line.Quantity)
                throw new InvalidOperationException(
                    $"Недостатньо товару {product.Name}. " +
                    $"Потрібно: {line.Quantity}, доступно: {product.AvailableQuantity}");
        }
    }
}