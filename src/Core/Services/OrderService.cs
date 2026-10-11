using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store =
        store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string customerId)
    {
        Order order = Order.Create(customerId);

        _store.Add(order);

        return order;
    }

    public void AddLine(
        string id,
        string productId,
        string name,
        decimal price,
        int quantity)
    {
        Order order = _store.GetById(id)
            ?? throw new InvalidOperationException(
                $"Немає замовлення з id={id}.");

        order.AddLine(
            productId,
            name,
            price,
            quantity);

        _store.Update(order);
    }

    public void Confirm(string id)
    {
        Order order = _store.GetById(id)
            ?? throw new InvalidOperationException(
                $"Немає замовлення з id={id}.");

        order.Confirm();

        _store.Update(order);
    }

    public IReadOnlyList<Order> All() =>
        _store.List();

    public Order? Find(string id) =>
        _store.GetById(id);
}