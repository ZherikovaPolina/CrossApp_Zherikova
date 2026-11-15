namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public OrdersOptions Orders { get; set; } = new();

    public string EnvironmentName { get; set; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; set; } = "data";
    public string FileName { get; set; } = "orders.json";

    public string ResolveFullPath(string baseDir)
    {
        var root = Path.IsPathRooted(DataPath)
            ? DataPath
            : Path.Combine(baseDir, DataPath);

        Directory.CreateDirectory(root);

        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; set; } = "Information";
}

public sealed class OrdersOptions
{
    public string Currency { get; set; } = "UAH";
}
