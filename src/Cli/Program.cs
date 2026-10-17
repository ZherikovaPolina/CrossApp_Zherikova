using System.Text;
using Core;
using Core.Abstractions;
using Core.Dto;
using Core.Import;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

Console.WriteLine("CrossApp – інформація про середовище");
Console.WriteLine();

Console.WriteLine("Студентка: Жерікова Поліна, група ФЕІ-36");
Console.WriteLine();

Console.WriteLine(new string('-', 60));

Console.WriteLine($"ОС : {report.OsDescription}");
Console.WriteLine($"Runtime : {report.FrameworkDescription}");
Console.WriteLine($"Архітектура : {report.ProcessArchitecture}");
Console.WriteLine($"RID (визначено): {report.DetectedRid}");
Console.WriteLine($"RID (від .NET) : {report.ReportedRid}");
Console.WriteLine($"Каталог : {report.BaseDirectory}");
Console.WriteLine($"Примітка збірки : {report.BuildNote}");

Console.WriteLine(new string('-', 60));
Console.WriteLine();

string? inputPath = args
    .FirstOrDefault(arg => !arg.Equals(
        "--file",
        StringComparison.OrdinalIgnoreCase));

string path = inputPath
    ?? Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

if (Path.GetFileName(path).Equals(
    "mixed.csv",
    StringComparison.OrdinalIgnoreCase))
{
    ImportResult<object> mixedResult = MixedCsvImporter.Load(path);

    Console.WriteLine("Результати mixed.csv:");

    foreach (object item in mixedResult.Items)
    {
        switch (item)
        {
            case ProductDto product:
                Console.WriteLine(
                    $"Product: {product.Id} {product.Name} {product.Price:F2}");
                break;

            case OrderDto order:
                Console.WriteLine(
                    $"Order: {order.Id} {order.CustomerId}");

                foreach (OrderLineDto line in order.Lines)
                {
                    Console.WriteLine(
                        $"  {line.ProductId} {line.Name} " +
                        $"{line.Price:F2} {line.Quantity}");
                }

                break;
        }
    }

    Console.WriteLine();

    Console.WriteLine(
        $"Кількість помилок: {mixedResult.Errors.Count}");

    foreach (string error in mixedResult.Errors)
    {
        Console.WriteLine($" ! {error}");
    }

    return 0;
}

ImportResult<ProductDto>? result = extension switch
{
    ".csv" => ProductCsvImporter.Load(path),

    ".json" => ProductJsonImporter.Load(path),

    _ => null
};

if (result is null)
{
    Console.WriteLine(
        $"Непідтримуваний формат файлу: {extension}");

    Console.WriteLine(
        "Підтримуються формати: .csv та .json");

    return 1;
}

Console.WriteLine(
    $"Завантажено записів: {result.Items.Count}");

foreach (ProductDto p in result.Items.Take(5))
{
    Console.WriteLine(
        $" {p.Id,-6} {p.Name,-26} {p.Price,10:F2}");
}

int total = result.Items.Count + result.Errors.Count;
int skipped = result.Errors.Count;

double errorPercent = total == 0
    ? 0
    : skipped * 100.0 / total;

Console.WriteLine(
    $"Статистика: усього {total}, " +
    $"прийнято {result.Items.Count}, " +
    $"пропущено {skipped}, " +
    $"помилки {errorPercent:F1}%");

foreach (string e in result.Errors)
{
    Console.WriteLine($" ! {e}");
}


Console.WriteLine();
Console.WriteLine(new string('-', 60));

Console.WriteLine("=== Сценарій 1: успіх ===");

Order newOrder = Order.Create("C001");

newOrder.AddLine(
    "P001",
    "Ноутбук",
    25000m,
    2);

newOrder.AddLine(
    "P002",
    "Мишка",
    800m,
    1);

Console.WriteLine($"Замовлення: {newOrder.Id}");
Console.WriteLine($"Клієнт: {newOrder.CustomerId}");
Console.WriteLine(
    $"Кількість рядків: {newOrder.Lines.Count}");
Console.WriteLine(
    $"Загальна сума: {newOrder.Total:F2}");

newOrder.Confirm();

Console.WriteLine($"Статус: {newOrder.Status}");

OrderDto dto = newOrder.ToDto();
Order restoredOrder = Order.FromDto(dto);

Console.WriteLine();
Console.WriteLine("Після ToDto / FromDto:");

Console.WriteLine(
    $"ID збережено: {restoredOrder.Id == newOrder.Id}");

Console.WriteLine(
    $"Клієнт: {restoredOrder.CustomerId}");

Console.WriteLine(
    $"Кількість рядків: {restoredOrder.Lines.Count}");

Console.WriteLine(
    $"Загальна сума: {restoredOrder.Total:F2}");

Console.WriteLine(
    $"Статус: {restoredOrder.Status}");

Console.WriteLine();
Console.WriteLine(
    "=== Сценарій 2: порушення інваріантів ===");

TryDo(
    "додавання рядка після підтвердження",
    () => newOrder.AddLine(
        "P003",
        "Клавіатура",
        1500m,
        1));

TryDo(
    "нульова кількість",
    () =>
    {
        Order testOrder = Order.Create("C002");

        testOrder.AddLine(
            "P004",
            "Монітор",
            10000m,
            0);
    });

TryDo(
    "від'ємна ціна",
    () =>
    {
        Order testOrder = Order.Create("C004");

        testOrder.AddLine(
            "P005",
            "Навушники",
            -100m,
            1);
    });

TryDo(
    "підтвердження порожнього замовлення",
    () =>
    {
        Order emptyOrder = Order.Create("C003");
        emptyOrder.Confirm();
    });

Console.WriteLine();
Console.WriteLine(
    "Стан замовлення після невдалих операцій:");

Console.WriteLine(
    $"Кількість рядків: {newOrder.Lines.Count}");

Console.WriteLine(
    $"Загальна сума: {newOrder.Total:F2}");

Console.WriteLine(
    $"Статус: {newOrder.Status}");

Console.WriteLine();
Console.WriteLine(new string('-', 60));

var importedOrders = new List<OrderDto>
{
    new OrderDto(
        "O001",
        "C001",
        OrderStatus.Draft,
        new List<OrderLineDto>
        {
            new(
                "P001",
                "Ноутбук",
                25000m,
                1)
        }),

    new OrderDto(
        "O002",
        "C002",
        OrderStatus.Draft,
        new List<OrderLineDto>
        {
            new(
                "P002",
                "Мишка",
                800m,
                0)
        }),

    new OrderDto(
        "O003",
        "C003",
        OrderStatus.Draft,
        new List<OrderLineDto>
        {
            new(
                "P003",
                "Навушники",
                -100m,
                1)
        })
};

var importResult = new ImportResult<OrderDto>(
    importedOrders,
    new List<string>());

ImportResult<Order> domainResult =
    OrderImportMapper.ToDomain(importResult);

Console.WriteLine(
    $"Створено сутностей: {domainResult.Items.Count}");

foreach (Order order in domainResult.Items)
{
    Console.WriteLine(
        $"Order: {order.Id}, " +
        $"клієнт: {order.CustomerId}, " +
        $"сума: {order.Total:F2}");
}

Console.WriteLine(
    $"Не пройшли інваріанти: {domainResult.Errors.Count}");

foreach (string error in domainResult.Errors)
{
    Console.WriteLine($" ! {error}");
}

Console.WriteLine();
Console.WriteLine(new string('-', 60));

Order stockOrder = Order.Create("C005");

stockOrder.AddLine(
    "P101",
    "Ноутбук",
    30000m,
    2);

var enoughProducts = new List<Product>
{
    Product.Create(
        "P101",
        "Ноутбук",
        5)
};

Console.WriteLine(
    "Перевірка 1: товару достатньо");

try
{
    OrderRules.EnsureProductsAvailable(
        stockOrder,
        enoughProducts);

    Console.WriteLine(
        "Перевірка пройдена: товару достатньо");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(
        $"Помилка: {ex.Message}");
}

var notEnoughProducts = new List<Product>
{
    Product.Create(
        "P101",
        "Ноутбук",
        1)
};

Console.WriteLine();
Console.WriteLine(
    "Перевірка 2: товару недостатньо");

try
{
    OrderRules.EnsureProductsAvailable(
        stockOrder,
        notEnoughProducts);

    Console.WriteLine(
        "Перевірка пройдена");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(
        $"Помилка: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine(new string('-', 60));

Order statusOrder1 = Order.Create("C006");

statusOrder1.AddLine(
    "P201",
    "Ноутбук",
    30000m,
    1);

Console.WriteLine(
    $"Початковий стан: {statusOrder1.Status}");

statusOrder1.Confirm();

Console.WriteLine(
    $"Після Confirm(): {statusOrder1.Status}");

Order statusOrder2 = Order.Create("C007");

statusOrder2.AddLine(
    "P202",
    "Мишка",
    800m,
    1);

Console.WriteLine();

Console.WriteLine(
    $"Початковий стан другого замовлення: " +
    $"{statusOrder2.Status}");

statusOrder2.Cancel();

Console.WriteLine(
    $"Після Cancel(): {statusOrder2.Status}");

try
{
    statusOrder2.Confirm();

    Console.WriteLine(
        "Помилка: заборонений перехід був виконаний");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(
        $"Заборонений перехід: {ex.Message}");
}

// ------------------------------------------------------------
// Лабораторна робота №5
// ------------------------------------------------------------

Console.WriteLine();
Console.WriteLine(new string('-', 60));
Console.WriteLine("=== Лабораторна робота №5 ===");

IOrderStore baseStore =
    StoreFactory.Create(args);

bool useCache = args.Contains(
    "--cache",
    StringComparer.OrdinalIgnoreCase);

IOrderStore store = useCache
    ? new CachingOrderStore(baseStore)
    : baseStore;

var service = new OrderService(store);

Console.WriteLine(
    $"Сховище: {baseStore.GetType().Name}");

Console.WriteLine(
    $"Кешування: {(useCache ? "увімкнено" : "вимкнено")}");

Console.WriteLine(
    $"Робоче сховище: {store.GetType().Name}");

try
{
    Order created =
        service.CreateOrder("C008");

    service.AddLine(
        created.Id,
        "P301",
        "Клавіатура",
        1500m,
        1);

    service.Confirm(created.Id);

    Console.WriteLine();
    Console.WriteLine("Список замовлень:");

    foreach (Order order in service.All())
    {
        Console.WriteLine(
            $"{order.Id} | " +
            $"клієнт: {order.CustomerId} | " +
            $"статус: {order.Status} | " +
            $"сума: {order.Total:F2}");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Пошук створеного замовлення:");

    Order? found =
        service.Find(created.Id);

    if (found is not null)
    {
        Console.WriteLine(
            $"{found.Id} | " +
            $"клієнт: {found.CustomerId} | " +
            $"статус: {found.Status} | " +
            $"рядків: {found.Lines.Count}");
    }
}
catch (Exception ex)
{
    Console.WriteLine(
        $"Помилка: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine(
    "Пошук підтверджених замовлень:");

IReadOnlyList<Order> confirmedOrders =
    service.Search(
        order => order.Status == OrderStatus.Confirmed);

foreach (Order order in confirmedOrders)
{
    Console.WriteLine(
        $"{order.Id} | " +
        $"клієнт: {order.CustomerId} | " +
        $"статус: {order.Status} | " +
        $"сума: {order.Total:F2}");
}

Console.WriteLine();
Console.WriteLine(
    "Пошук замовлень із сумою понад 100000:");

IReadOnlyList<Order> expensiveOrders =
    service.Search(
        order => order.Total > 100000m);

if (expensiveOrders.Count == 0)
{
    Console.WriteLine(
        "Замовлень за заданою умовою не знайдено.");
}
else
{
    foreach (Order order in expensiveOrders)
    {
        Console.WriteLine(
            $"{order.Id} | " +
            $"клієнт: {order.CustomerId} | " +
            $"сума: {order.Total:F2}");
    }
}

Console.WriteLine();
Console.WriteLine(
    "Перевірка некоректного параметра пошуку:");

try
{
    service.Search(null!);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine(
        $"Очікувана помилка: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Сценарій відмови:");

try
{
    service.AddLine(
        "ORDER-NOT-FOUND",
        "P999",
        "Тестовий товар",
        100m,
        1);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(
        $"Очікувана помилка: {ex.Message}");
}

static void TryDo(
    string title,
    Action action)
{
    try
    {
        action();

        Console.WriteLine(
            $"{title}: виняток НЕ спрацював — " +
            "інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"{title}: {ex.Message}");
    }
}

return 0;