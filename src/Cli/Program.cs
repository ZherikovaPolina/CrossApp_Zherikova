using System.Text;
using Core;
using Core.Dto;
using Core.Import;
using Core.Domain;

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

string path = args.Length > 0
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

if (Path.GetFileName(path).Equals("mixed.csv", StringComparison.OrdinalIgnoreCase))
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
            $"  {line.ProductId} {line.Name} {line.Price:F2} {line.Quantity}");
    }

    break;
        }
    }

    Console.WriteLine();

    Console.WriteLine($"Кількість помилок: {mixedResult.Errors.Count}");

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
    Console.WriteLine($"Непідтримуваний формат файлу: {extension}");
    Console.WriteLine("Підтримуються формати: .csv та .json");
    return 1;
}

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (ProductDto p in result.Items.Take(5))
{
    Console.WriteLine($" {p.Id,-6} {p.Name,-26} {p.Price,10:F2}");
}

int total = result.Items.Count + result.Errors.Count;
int skipped = result.Errors.Count;
double errorPercent = total == 0
    ? 0
    : skipped * 100.0 / total;

Console.WriteLine(
    $"Статистика: усього {total}, прийнято {result.Items.Count}, " +
    $"пропущено {skipped}, помилки {errorPercent:F1}%");

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
Console.WriteLine($"Кількість рядків: {newOrder.Lines.Count}");
Console.WriteLine($"Загальна сума: {newOrder.Total:F2}");

newOrder.Confirm();

Console.WriteLine($"Підтверджено: {newOrder.IsConfirmed}");

OrderDto dto = newOrder.ToDto();
Order restoredOrder = Order.FromDto(dto);

Console.WriteLine();
Console.WriteLine("Після ToDto / FromDto:");
Console.WriteLine($"ID збережено: {restoredOrder.Id == newOrder.Id}");
Console.WriteLine($"Клієнт: {restoredOrder.CustomerId}");
Console.WriteLine($"Кількість рядків: {restoredOrder.Lines.Count}");
Console.WriteLine($"Загальна сума: {restoredOrder.Total:F2}");
Console.WriteLine($"Підтверджено: {restoredOrder.IsConfirmed}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

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
Console.WriteLine("Стан замовлення після невдалих операцій:");
Console.WriteLine($"Кількість рядків: {newOrder.Lines.Count}");
Console.WriteLine($"Загальна сума: {newOrder.Total:F2}");
Console.WriteLine($"Підтверджено: {newOrder.IsConfirmed}");

static void TryDo(string title, Action action)
{
    try
    {
        action();

        Console.WriteLine(
            $"{title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"{title}: {ex.Message}");
    }
}
return 0;