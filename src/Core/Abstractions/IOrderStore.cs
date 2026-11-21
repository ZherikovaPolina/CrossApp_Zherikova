using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Order> List() =>
        ListAsync().GetAwaiter().GetResult();

    Order? GetById(string id) =>
        GetByIdAsync(id).GetAwaiter().GetResult();

    void Add(Order item) =>
        AddAsync(item).GetAwaiter().GetResult();

    void Update(Order item) =>
        UpdateAsync(item).GetAwaiter().GetResult();

    bool Remove(string id) =>
        RemoveAsync(id).GetAwaiter().GetResult();

    Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order item,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Order item,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(
        string id,
        CancellationToken cancellationToken = default);
}
