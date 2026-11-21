using System.Collections.Concurrent;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly ConcurrentDictionary<string, Order> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return _inner.ListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue(id, out Order? cached))
            return cached;

        Order? order =
            await _inner.GetByIdAsync(id, cancellationToken);

        if (order is not null)
            _cache[id] = order;

        return order;
    }

    public async Task AddAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _inner.AddAsync(item, cancellationToken);
        _cache[item.Id] = item;
    }

    public async Task UpdateAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _inner.UpdateAsync(item, cancellationToken);
        _cache[item.Id] = item;
    }

    public async Task<bool> RemoveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        bool removed =
            await _inner.RemoveAsync(id, cancellationToken);

        if (removed)
            _cache.TryRemove(id, out _);

        return removed;
    }
}
