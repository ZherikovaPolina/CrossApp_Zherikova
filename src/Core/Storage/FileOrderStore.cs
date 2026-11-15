using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using System.Text.Encodings.Web;

namespace Core.Storage;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new()
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

    private readonly Dictionary<string, Order> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _path =
        Path.GetFullPath(path);

    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        if (File.Exists(_path))
        {
            var dtos =
                JsonSerializer.Deserialize<List<OrderDto>>(
                    File.ReadAllText(_path)) ?? [];

            foreach (OrderDto dto in dtos)
            {
                Order order = Order.FromDto(dto);
                _cache[order.Id] = order;
            }
        }

        _loaded = true;
    }


private void Flush()
{

    Directory.CreateDirectory(
        Path.GetDirectoryName(_path)!);

    string tempPath = _path + ".tmp";

    var dtos = _cache.Values
        .Select(order => order.ToDto())
        .ToList();

    string json = JsonSerializer.Serialize(dtos, Options);

    try
    {
        File.WriteAllText(tempPath, json);

        File.Move(tempPath, _path, overwrite: true);
    }
    finally
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }
}


    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();

        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();

        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);

        EnsureLoaded();

        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException(
                $"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);

        Flush();
    }

    public void Update(Order item)
    {
        EnsureLoaded();

        _cache[item.Id] = item;

        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();

        if (!_cache.Remove(id))
            return false;

        Flush();

        return true;
    }
}