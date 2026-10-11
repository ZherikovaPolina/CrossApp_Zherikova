using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool useFile = args.Contains(
            "--file",
            StringComparer.OrdinalIgnoreCase);

        string dataPath = Path.Combine(
            "data",
            "orders.json");

        return useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(SampleData.Orders());
    }
}