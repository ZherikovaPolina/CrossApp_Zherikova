using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Notifications;

namespace Core.Services;

public sealed class OrderService(
    IOrderStore store,
    INotificationSink? sink = null)
{
    private readonly IOrderStore _store =
        store ?? throw new ArgumentNullException(nameof(store));

    private readonly INotificationSink? _sink = sink;

    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(
        Order order,
        OrderChangeKind kind)
    {
        var eventArgs =
            new OrderChangedEventArgs(
                order.Id,
                kind,
                order.Lines.Count,
                order.Total);

        Changed?.Invoke(
            this,
            eventArgs);

        _sink?.Notify(
            $"{kind}: замовлення {order.Id}, " +
            $"рядків: {order.Lines.Count}, " +
            $"сума: {order.Total:F2}");
    }

    public Order CreateOrder(string customerId)
    {
        Order order = Order.Create(customerId);

        _store.Add(order);

        OnChanged(
            order,
            OrderChangeKind.OrderCreated);

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

        OnChanged(
            order,
            OrderChangeKind.LineAdded);
    }

    public void Confirm(string id)
    {
        Order order = _store.GetById(id)
            ?? throw new InvalidOperationException(
                $"Немає замовлення з id={id}.");

        order.Confirm();

        _store.Update(order);

        OnChanged(
            order,
            OrderChangeKind.OrderConfirmed);
    }

    public IReadOnlyList<Order> All() =>
        _store.List();

    public Order? Find(string id) =>
        _store.GetById(id);

    public IReadOnlyList<Order> FindAll(
        Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _store.List()
            .Where(predicate)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<Order> Search(
        Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _store.List()
            .Where(predicate)
            .ToList()
            .AsReadOnly();
    }
}