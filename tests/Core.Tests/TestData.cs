
using Core.Domain;

namespace Core.Tests;

public static class TestData
{
    public static IReadOnlyList<Order> Items()
    {
        var order1 = Order.Create("C001");
        order1.AddLine("P001", "Ноутбук", 10000m, 1);

        var order2 = Order.Create("C002");
        order2.AddLine("P002", "Мишка", 500m, 1);

        var order3 = Order.Create("C003");
        order3.AddLine("P003", "Монітор", 5000m, 1);

        return new List<Order>
        {
            order1,
            order2,
            order3
        };
    }

    public static IReadOnlyList<Order> TopCustomersItems()
    {
        var order1 = Order.Create("C-BIG");
        order1.AddLine("P001", "Ноутбук", 10000m, 1);

        var order2 = Order.Create("C-MID");
        order2.AddLine("P002", "Монітор", 5000m, 1);

        var order3 = Order.Create("C-SMALL");
        order3.AddLine("P003", "Мишка", 500m, 1);

        return new List<Order>
        {
            order1,
            order2,
            order3
        };
    }
}
