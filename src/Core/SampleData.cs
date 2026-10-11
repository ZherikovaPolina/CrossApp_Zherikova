using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Order> Orders()
    {
        var orders = new List<Order>();

        for (int i = 1; i <= 15; i++)
        {
            Order order = Order.Create(
                $"CUSTOMER-{i:000}");

            order.AddLine(
                $"PRODUCT-{i:000}",
                $"Товар {i}",
                100m * i,
                i);

            orders.Add(order);
        }

        return orders.AsReadOnly();
    }
}