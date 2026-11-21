using System.Collections.Concurrent;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly ConcurrentDictionary<string, Order> _items =
        new(StringComparer.OrdinalIgnoreCase);

    public InMemoryOrderStore()
    {
    }

    public InMemoryOrderStore(IEnumerable<Order> seed)
    {
        foreach (var item in seed)
        {
            _items.TryAdd(item.Id, item);
        }
    }

    public Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Order> orders = _items.Values.ToList();
        return Task.FromResult(orders);
    }

    public Task<Order?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _items.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Order? GetById(string id)
{
    _items.TryGetValue(id, out var order);
    return order;
}

    public Task AddAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_items.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException(
                $"Order with ID '{item.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool removed = _items.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}
