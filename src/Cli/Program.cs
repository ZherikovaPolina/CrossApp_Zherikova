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
    Domain = "Замовлення"
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

    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"ОС (OSDescription) : {info.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {info.EnvironmentOS}");
    Console.WriteLine($"Архітектура процесу : {info.Architecture}");
    Console.WriteLine($"Версія .NET (CLR) : {info.DotNetVersion}");
    Console.WriteLine($"Runtime : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку : {info.ApplicationDirectory}");
    Console.WriteLine($"Поточний каталог : {info.CurrentDirectory}");

    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"Предметна область: {info.Domain} (клієнти, товари, замовлення, рядки замовлень)");
}