using System.Text.Json;
using System.Text.Encodings.Web;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

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

    private readonly string _path = Path.GetFullPath(path);

    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _loaded;

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded)
            return;

        if (File.Exists(_path))
        {
            string json = await File.ReadAllTextAsync(_path, ct);

            var dtos =
                JsonSerializer.Deserialize<List<OrderDto>>(json) ?? [];

            foreach (OrderDto dto in dtos)
            {
                Order order = Order.FromDto(dto);
                _cache[order.Id] = order;
            }
        }

        _loaded = true;
    }

    private async Task FlushAsync(CancellationToken ct)
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
            await File.WriteAllTextAsync(tempPath, json, ct);

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public async Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _cache.Values.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Order?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _cache.GetValueOrDefault(id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (_cache.ContainsKey(item.Id))
                throw new InvalidOperationException(
                    $"Запис з id={item.Id} уже існує.");

            _cache.Add(item.Id, item);
            await FlushAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);

            _cache[item.Id] = item;
            await FlushAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> RemoveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!_cache.Remove(id))
                return false;

            await FlushAsync(cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
