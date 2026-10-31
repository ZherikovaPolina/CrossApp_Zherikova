using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Order> Orders()
    {
        var orders = new List<Order>();

        Order order1 = CreateOrder(
            "CUSTOMER-001",
            "PRODUCT-001",
            "Ноутбук",
            25000m,
            1);
        order1.Confirm();
        orders.Add(order1);

        Order order2 = CreateOrder(
            "CUSTOMER-002",
            "PRODUCT-002",
            "Мишка",
            800m,
            2);
        order2.Confirm();
        orders.Add(order2);

        Order order3 = CreateOrder(
            "CUSTOMER-001",
            "PRODUCT-003",
            "Монітор",
            12000m,
            1);
        order3.Confirm();
        orders.Add(order3);

        Order order4 = CreateOrder(
            "CUSTOMER-003",
            "PRODUCT-004",
            "Клавіатура",
            1800m,
            1);
        orders.Add(order4);

        Order order5 = CreateOrder(
            "CUSTOMER-002",
            "PRODUCT-005",
            "Навушники",
            3000m,
            1);
        order5.Cancel();
        orders.Add(order5);

        Order order6 = CreateOrder(
            "CUSTOMER-004",
            "PRODUCT-006",
            "Планшет",
            15000m,
            1);
        order6.Confirm();
        orders.Add(order6);

        Order order7 = CreateOrder(
            "CUSTOMER-005",
            "PRODUCT-007",
            "Кабель",
            500m,
            2);
        orders.Add(order7);

        Order order8 = CreateOrder(
            "CUSTOMER-003",
            "PRODUCT-008",
            "Вебкамера",
            2200m,
            1);
        order8.Confirm();
        orders.Add(order8);

        Order order9 = CreateOrder(
            "CUSTOMER-004",
            "PRODUCT-009",
            "Принтер",
            9000m,
            1);
        order9.Cancel();
        orders.Add(order9);

        Order order10 = CreateOrder(
            "CUSTOMER-005",
            "PRODUCT-010",
            "Флешка",
            600m,
            3);
        order10.Confirm();
        orders.Add(order10);

        Order order11 = CreateOrder(
            "CUSTOMER-001",
            "PRODUCT-011",
            "Смартфон",
            18000m,
            1);
        order11.Confirm();
        orders.Add(order11);

        Order order12 = CreateOrder(
            "CUSTOMER-006",
            "PRODUCT-012",
            "Колонки",
            3500m,
            1);
        orders.Add(order12);

        Order order13 = CreateOrder(
            "CUSTOMER-006",
            "PRODUCT-013",
            "Мікрофон",
            2500m,
            2);
        order13.Confirm();
        orders.Add(order13);

        Order order14 = CreateOrder(
            "CUSTOMER-007",
            "PRODUCT-014",
            "SSD",
            4500m,
            1);
        order14.Cancel();
        orders.Add(order14);

        Order order15 = CreateOrder(
            "CUSTOMER-007",
            "PRODUCT-015",
            "Роутер",
            3200m,
            1);
        order15.Confirm();
        orders.Add(order15);

        return orders.AsReadOnly();
    }       

    public static IReadOnlyList<Product> Products()
    {
        var products = new List<Product>
        {
            Product.Create("PRODUCT-001", "Ноутбук", 5),
            Product.Create("PRODUCT-002", "Мишка", 20),
            Product.Create("PRODUCT-003", "Монітор", 8),
            Product.Create("PRODUCT-004", "Клавіатура", 15),
            Product.Create("PRODUCT-005", "Навушники", 12),
            Product.Create("PRODUCT-006", "Планшет", 6),
            Product.Create("PRODUCT-007", "Кабель", 30),
            Product.Create("PRODUCT-008", "Вебкамера", 10),
            Product.Create("PRODUCT-009", "Принтер", 4),
            Product.Create("PRODUCT-010", "Флешка", 25),
            Product.Create("PRODUCT-011", "Смартфон", 7),
            Product.Create("PRODUCT-012", "Колонки", 9),
            Product.Create("PRODUCT-013", "Мікрофон", 11),
            Product.Create("PRODUCT-014", "SSD", 14),
            Product.Create("PRODUCT-015", "Роутер", 8),
            Product.Create("PRODUCT-016", "Геймпад", 10)
        };

        return products.AsReadOnly();
    }

    private static Order CreateOrder(
        string customerId,
        string productId,
        string name,
        decimal price,
        int quantity)
    {
        Order order =
            Order.Create(customerId);

        order.AddLine(
            productId,
            name,
            price,
            quantity);

        return order;
    }
}