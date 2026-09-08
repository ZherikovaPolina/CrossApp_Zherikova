using System.Runtime.InteropServices;
using System.Text.Json;

var info = new
{
    OSDescription = RuntimeInformation.OSDescription,
    EnvironmentOS = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    ApplicationDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення(клієнти, товари, замовлення, рядки замовлень)"
};

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine();
    Console.WriteLine("Студентка: Жерікова Поліна, група ФЕІ-36");
    Console.WriteLine();

    Console.WriteLine(new string('-', 90));
    Console.WriteLine("{0,-30} {1}", "Параметр", "Значення");
    Console.WriteLine(new string('-', 90));

    Console.WriteLine("{0,-30} {1}", "OSDescription", info.OSDescription);
    Console.WriteLine("{0,-30} {1}", "ОС (Environment)", info.EnvironmentOS);
    Console.WriteLine("{0,-30} {1}", "Архітектура процесу", info.Architecture);
    Console.WriteLine("{0,-30} {1}", "Версія .NET (CLR)", info.DotNetVersion);
    Console.WriteLine("{0,-30} {1}", "Runtime", info.Runtime);
    Console.WriteLine("{0,-30} {1}", "Каталог застосунку", info.ApplicationDirectory);
    Console.WriteLine("{0,-30} {1}", "Поточний каталог", info.CurrentDirectory);
    Console.WriteLine("{0,-30} {1}", "Предметна область", info.Domain);

    Console.WriteLine(new string('-', 90));
}