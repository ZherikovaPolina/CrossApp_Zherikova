using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Core;

Console.OutputEncoding = Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

var jsonOptions = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));
}
else
{
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
}