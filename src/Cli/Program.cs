using System.Text;
using Core;
using Core.Abstractions;
using Core.Dto;
using Core.Import;
using Core.Domain;
using Core.Services;
using Core.Storage;
using Core.Events;
using Cli.Notifications;
using Core.Notifications;
using Core.Reports;
using System.Diagnostics;

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

string? reportCommand = args
    .FirstOrDefault(arg =>
        !arg.StartsWith("--") &&
        arg.StartsWith(
            "report-",
            StringComparison.OrdinalIgnoreCase));

if (reportCommand is not null)
{
    RunLab7Report(reportCommand);
    return 0;
}

string? inputPath = args
    .FirstOrDefault(arg =>
        !arg.StartsWith("--") &&
        !arg.StartsWith(
            "report-",
            StringComparison.OrdinalIgnoreCase));

string path = inputPath
    ?? Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine(
        $"Файл не знайдено: {Path.GetFullPath(path)}");

    return 1;
}

string extension =
    Path.GetExtension(path).ToLowerInvariant();

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

// ------------------------------------------------------------
// Лабораторна робота №6
// ------------------------------------------------------------

Console.WriteLine();
Console.WriteLine(new string('-', 60));
Console.WriteLine("=== Лабораторна робота №6 ===");

IOrderStore lab6Store =
    new InMemoryOrderStore();

var lab6Service =
    new OrderService(lab6Store);

var logPath = Path.Combine(
    AppContext.BaseDirectory,
    "logs",
    "app.log");

Directory.CreateDirectory(
    Path.GetDirectoryName(logPath)!);

void OnOrderChanged(
    object? sender,
    OrderChangedEventArgs e)
{
    var line =
        $"{e.At:HH:mm:ss} " +
        $"{e.Kind,-14} " +
        $"{e.OrderId} | " +
        $"рядків: {e.LinesCount} | " +
        $"сума: {e.Total:F2}";

    try
    {
        Console.WriteLine(line);

        File.AppendAllText(
            logPath,
            line + Environment.NewLine);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"Помилка журналювання: {ex.Message}");
    }
}

lab6Service.Changed += OnOrderChanged;

int changes = 0;

lab6Service.Changed += (_, _) => changes++;

Console.WriteLine();
Console.WriteLine(
    "Події зміни замовлення:");

Order lab6Order =
    lab6Service.CreateOrder("C-LAB6");

lab6Service.AddLine(
    lab6Order.Id,
    "P601",
    "Навушники",
    2000m,
    1);

lab6Service.Confirm(
    lab6Order.Id);

Console.WriteLine(
    $"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine(
    "Перевірка відписки:");

lab6Service.Changed -= OnOrderChanged;

Order silentOrder =
    lab6Service.CreateOrder("C-SILENT");

Console.WriteLine(
    $"Подій отримано: {changes}");

Console.WriteLine(
    $"Нове замовлення створено: {silentOrder.Id}");

Console.WriteLine();
Console.WriteLine(
    "Сценарій відмови:");

int changesBeforeFailure =
    changes;

try
{
    lab6Service.AddLine(
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

Console.WriteLine(
    $"Подій до помилки: {changesBeforeFailure}");

Console.WriteLine(
    $"Подій після помилки: {changes}");

Console.WriteLine();
Console.WriteLine(
    "Пошук через Func<Order, bool>:");

IReadOnlyList<Order> foundOrders =
    lab6Service.FindAll(
        order => order.Total < 1000m);

foreach (Order order in foundOrders)
{
    Console.WriteLine(
        $"{order.Id} | " +
        $"клієнт: {order.CustomerId} | " +
        $"сума: {order.Total:F2}");
}

Console.WriteLine();
Console.WriteLine(
    "Додаткове завдання №1 — INotificationSink:");

var sinkLogPath = Path.Combine(
    AppContext.BaseDirectory,
    "logs",
    "sink.log");

INotificationSink consoleSink =
    new ConsoleSink();

INotificationSink fileSink =
    new FileSink(sinkLogPath);

IOrderStore consoleStore =
    new InMemoryOrderStore();

var consoleSinkService =
    new OrderService(
        consoleStore,
        consoleSink);

Console.WriteLine(
    "ConsoleSink:");

Order consoleOrder =
    consoleSinkService.CreateOrder(
        "C-SINK-CONSOLE");

consoleSinkService.AddLine(
    consoleOrder.Id,
    "P701",
    "Вебкамера",
    1800m,
    1);

IOrderStore fileStore =
    new InMemoryOrderStore();

var fileSinkService =
    new OrderService(
        fileStore,
        fileSink);

Console.WriteLine();
Console.WriteLine(
    "FileSink:");

Order fileOrder =
    fileSinkService.CreateOrder(
        "C-SINK-FILE");

fileSinkService.AddLine(
    fileOrder.Id,
    "P702",
    "Мікрофон",
    2500m,
    1);

Console.WriteLine(
    $"Повідомлення записано у: {sinkLogPath}");

Console.WriteLine();
Console.WriteLine(
    "Додаткове завдання №2 — Action<string>:");

var skippedMessages =
    new List<string>();

ProductCsvImporter.Load(
    path,
    message =>
    {
        skippedMessages.Add(message);

        Console.WriteLine(
            $"Пропущено: {message}");
    });

Console.WriteLine(
    $"Усього пропущено рядків: {skippedMessages.Count}");


// ------------------------------------------------------------
// Лабораторна робота №7
// ------------------------------------------------------------

Console.WriteLine();
Console.WriteLine(new string('=', 60));
Console.WriteLine("=== Лабораторна робота №7 ===");
Console.WriteLine("ОСНОВНА ЧАСТИНА");
Console.WriteLine("Звіти через LINQ");
Console.WriteLine(new string('=', 60));

Console.WriteLine();
Console.WriteLine(
    "Демонстрація відкладеного виконання:");

var lab7Store =
    new InMemoryOrderStore();

var lab7Service =
    new OrderService(lab7Store);

Order lab7Order1 =
    lab7Service.CreateOrder("C-LAB7-1");

lab7Service.AddLine(
    lab7Order1.Id,
    "P801",
    "Ноутбук",
    25000m,
    1);

Order lab7Order2 =
    lab7Service.CreateOrder("C-LAB7-2");

lab7Service.AddLine(
    lab7Order2.Id,
    "P802",
    "Мишка",
    800m,
    1);

var source =
    lab7Service.All().ToList();

var query =
    ReportService.OrdersBelowTotal(
        source,
        1000m);

Console.WriteLine(
    $"Замовлень до змін: {query.Count()}");

Order addedOrder =
    Order.Create("C-LAB7-3");

addedOrder.AddLine(
    "P803",
    "Кабель",
    500m,
    1);

source.Add(addedOrder);

Console.WriteLine(
    $"Замовлень після змін: {query.Count()}");

var snapshot =
    query.ToList();

source.Clear();

Console.WriteLine(
    $"У знімку залишилось: {snapshot.Count}");

Console.WriteLine(new string('=', 60));

static void RunLab7Report(string command)
{
    IReadOnlyList<Order> orders =
        SampleData.Orders();

    Console.WriteLine();
    Console.WriteLine(new string('=', 60));
    Console.WriteLine("=== Лабораторна робота №7 ===");

    switch (command.ToLowerInvariant())
    {
        // ----------------------------------------------------
        // ОСНОВНА ЧАСТИНА
        // ----------------------------------------------------

        case "report-top":
        {
            Console.WriteLine("ОСНОВНА ЧАСТИНА");
            Console.WriteLine("Звіти через LINQ");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine();
            Console.WriteLine(
                "Топ-5 клієнтів за загальною сумою замовлень:");

            IReadOnlyList<CustomerSummary> customers =
                ReportService.TopCustomersByTotal(
                    orders,
                    5);

            foreach (CustomerSummary customer in customers)
            {
                Console.WriteLine(
                    $"{customer.CustomerId,-12} | " +
                    $"замовлень: {customer.OrdersCount,2} | " +
                    $"сума: {customer.Total,10:F2}");
            }

            break;
        }

        case "report-expensive":
        {
            Console.WriteLine("ОСНОВНА ЧАСТИНА");
            Console.WriteLine("Звіти через LINQ");
            Console.WriteLine(new string('=', 60));

            const decimal threshold = 5000m;

            Console.WriteLine();
            Console.WriteLine(
                $"Замовлення дорожче {threshold:F2}:");

            IReadOnlyList<OrderReportLine> expensive =
                ReportService.ExpensiveOrders(
                    orders,
                    threshold);

            foreach (OrderReportLine order in expensive)
            {
                Console.WriteLine(
                    $"{order.OrderId} | " +
                    $"клієнт: {order.CustomerId,-12} | " +
                    $"сума: {order.Total,10:F2}");
            }

            break;
        }

        case "report-groups":
        {
            Console.WriteLine("ОСНОВНА ЧАСТИНА");
            Console.WriteLine("Звіти через LINQ");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine();
            Console.WriteLine(
                "Групування замовлень за статусом:");

            IReadOnlyList<GroupSummary> groups =
                ReportService.ByStatus(orders);

            foreach (GroupSummary group in groups)
            {
                Console.WriteLine(
                    $"{group.Status,-10} | " +
                    $"замовлень: {group.Count,2} | " +
                    $"сума: {group.Total,10:F2}");
            }

            break;
        }

        case "report-sum":
        {
            Console.WriteLine("ОСНОВНА ЧАСТИНА");
            Console.WriteLine("Звіти через LINQ");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine();
            Console.WriteLine(
                "Загальна статистика замовлень:");

            var totals =
                ReportService.Totals(orders);

            Console.WriteLine(
                $"Кількість замовлень: {totals.Items}");

            Console.WriteLine(
                $"Загальна сума: {totals.Total:F2}");

            Console.WriteLine(
                $"Середній чек: {totals.Average:F2}");

            Console.WriteLine(
                $"Максимальне замовлення: {totals.Max:F2}");

            break;
        }

        // ----------------------------------------------------
        // ДОДАТКОВЕ ЗАВДАННЯ №1
        // GroupJoin двох колекцій
        // ----------------------------------------------------

        case "report-products":
        {
            Console.WriteLine("ДОДАТКОВЕ ЗАВДАННЯ №1");
            Console.WriteLine(
                "GroupJoin: товари × рядки замовлень");
            Console.WriteLine(new string('=', 60));

            IReadOnlyList<Product> products =
                SampleData.Products();

            IReadOnlyList<OrderLine> orderLines =
                orders
                    .SelectMany(order => order.Lines)
                    .ToList();

            IReadOnlyList<ProductOrdersSummary> summaries =
                ReportService.ProductOrders(
                    products,
                    orderLines);

            Console.WriteLine();
            Console.WriteLine(
                "Зведення товарів і рядків замовлень:");

            foreach (ProductOrdersSummary summary in summaries)
            {
                Console.WriteLine(
                    $"{summary.ProductId} | " +
                    $"{summary.ProductName,-12} | " +
                    $"на складі: {summary.AvailableQuantity,2} | " +
                    $"замовлено: {summary.OrderedQuantity,2} | " +
                    $"сума: {summary.OrderedTotal,10:F2}");
            }

            break;
        }

        // ----------------------------------------------------
        // ДОДАТКОВЕ ЗАВДАННЯ №2
        // Власний метод-розширення TopBy<T>
        // ----------------------------------------------------

        case "report-top-products":
        {
            Console.WriteLine("ДОДАТКОВЕ ЗАВДАННЯ №2");
            Console.WriteLine(
                "Власний метод-розширення TopBy<T>");
            Console.WriteLine(new string('=', 60));

            IReadOnlyList<Product> products =
                SampleData.Products();

            IReadOnlyList<OrderLine> orderLines =
                orders
                    .SelectMany(order => order.Lines)
                    .ToList();

            IReadOnlyList<ProductOrdersSummary> summaries =
                ReportService.ProductOrders(
                    products,
                    orderLines);

            IReadOnlyList<ProductOrdersSummary> topProducts =
                ReportService.TopProducts(
                    summaries,
                    5);

            Console.WriteLine();
            Console.WriteLine(
                "Топ-5 товарів за сумою замовлень:");

            foreach (ProductOrdersSummary product in topProducts)
            {
                Console.WriteLine(
                    $"{product.ProductId} | " +
                    $"{product.ProductName,-12} | " +
                    $"замовлено: {product.OrderedQuantity,2} | " +
                    $"сума: {product.OrderedTotal,10:F2}");
            }

            break;
        }

        // ----------------------------------------------------
        // ДОДАТКОВЕ ЗАВДАННЯ №3
        // LINQ у синтаксисі запитів
        // from ... group ... into ...
        // ----------------------------------------------------

        case "report-query":
        {
            Console.WriteLine("ДОДАТКОВЕ ЗАВДАННЯ №3");
            Console.WriteLine(
                "LINQ у синтаксисі запитів");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine();
            Console.WriteLine(
                "Групування замовлень за статусом:");
            Console.WriteLine(
                "(from ... group ... into ...)");

            IReadOnlyList<GroupSummary> groups =
                ReportService.ByStatusQuerySyntax(
                    orders);

            foreach (GroupSummary group in groups)
            {
                Console.WriteLine(
                    $"{group.Status,-10} | " +
                    $"замовлень: {group.Count,2} | " +
                    $"сума: {group.Total,10:F2}");
            }

            break;
        }

        // ----------------------------------------------------
// ДОДАТКОВЕ ЗАВДАННЯ №4
// Stopwatch: 100 000 записів
// ToList у середині ланцюжка і без нього
// ----------------------------------------------------

case "report-performance":
{
    Console.WriteLine("ДОДАТКОВЕ ЗАВДАННЯ №4");
    Console.WriteLine(
        "Stopwatch: порівняння LINQ на 100 000 записів");
    Console.WriteLine(new string('=', 60));

    const int count = 100_000;

    var random = new Random(5);

    List<PerformanceOrder> testData =
        Enumerable
            .Range(1, count)
            .Select(index => new PerformanceOrder(
                $"ORDER-{index:D6}",
                $"CUSTOMER-{random.Next(1, 1001):D4}",
                random.Next(100, 50001)))
            .ToList();

    Console.WriteLine();
    Console.WriteLine(
        $"Кількість згенерованих записів: {testData.Count}");

    var stopwatch = Stopwatch.StartNew();

    var withToList =
        testData
            .Where(order => order.Total > 5000m)
            .ToList()
            .GroupBy(order => order.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                Total = group.Sum(order => order.Total)
            })
            .OrderByDescending(result => result.Total)
            .Take(5)
            .ToList();

    stopwatch.Stop();

    long withToListTicks =
        stopwatch.ElapsedTicks;

    double withToListMs =
        stopwatch.Elapsed.TotalMilliseconds;

    stopwatch.Restart();

    var withoutToList =
        testData
            .Where(order => order.Total > 5000m)
            .GroupBy(order => order.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                Total = group.Sum(order => order.Total)
            })
            .OrderByDescending(result => result.Total)
            .Take(5)
            .ToList();

    stopwatch.Stop();

    long withoutToListTicks =
        stopwatch.ElapsedTicks;

    double withoutToListMs =
        stopwatch.Elapsed.TotalMilliseconds;

    Console.WriteLine();
    Console.WriteLine("Варіант 1 — ToList у середині ланцюжка:");
    Console.WriteLine(
        $"Час: {withToListMs:F3} мс " +
        $"({withToListTicks} ticks)");

    Console.WriteLine();
    Console.WriteLine("Варіант 2 — без проміжного ToList:");
    Console.WriteLine(
        $"Час: {withoutToListMs:F3} мс " +
        $"({withoutToListTicks} ticks)");

    Console.WriteLine();
    Console.WriteLine(
        $"Кількість результатів: " +
        $"{withToList.Count} / {withoutToList.Count}");

    Console.WriteLine();
    Console.WriteLine(
        "Топ-5 клієнтів:");

    foreach (var result in withoutToList)
    {
        Console.WriteLine(
            $"{result.CustomerId} | " +
            $"сума: {result.Total:F2}");
    }

    break;
}

        // ----------------------------------------------------
        // СПИСОК КОМАНД
        // ----------------------------------------------------

        default:
        {
            Console.WriteLine("Доступні команди");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine();
            Console.WriteLine("Основна частина:");
            Console.WriteLine(
                "report-top | report-expensive | " +
                "report-groups | report-sum");

            Console.WriteLine();
            Console.WriteLine("Додаткове завдання №1:");
            Console.WriteLine(
                "report-products");

            Console.WriteLine();
            Console.WriteLine("Додаткове завдання №2:");
            Console.WriteLine(
                "report-top-products");

            Console.WriteLine();
            Console.WriteLine("Додаткове завдання №3:");
            Console.WriteLine(
                "report-query");

            Console.WriteLine();
Console.WriteLine("Додаткове завдання №4:");
Console.WriteLine(
    "report-performance");

            break;
        }
    }

    Console.WriteLine(new string('=', 60));
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