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

ImportResult<ProductDto> result = ProductCsvImporter.Load(path);

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (ProductDto p in result.Items.Take(5))
{
Console.WriteLine($" {p.Id,-6} {p.Name,-26} {p.Price,10:F2}");
}

if (result.Errors.Count > 0)
{
Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");

foreach (string e in result.Errors)
{
    Console.WriteLine($" ! {e}");
}

}

return 0;
