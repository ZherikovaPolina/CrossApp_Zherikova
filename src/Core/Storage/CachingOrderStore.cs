using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly Dictionary<string, Order> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List() =>
        _inner.List();

    public Order? GetById(string id)
    {
        if (_cache.TryGetValue(id, out Order? cached))
            return cached;

        Order? order = _inner.GetById(id);

        if (order is not null)
            _cache[id] = order;

        return order;
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _inner.Add(item);
        _cache[item.Id] = item;
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _inner.Update(item);
        _cache[item.Id] = item;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);

        if (removed)
            _cache.Remove(id);

        return removed;
    }
}