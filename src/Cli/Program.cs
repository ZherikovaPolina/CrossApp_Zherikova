using System.Text;
using Core;
using Core.Dto;
using Core.Import;

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
                    $"Order: {order.Id} {order.CustomerId} {order.ProductId} {order.Quantity}");
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

return 0;