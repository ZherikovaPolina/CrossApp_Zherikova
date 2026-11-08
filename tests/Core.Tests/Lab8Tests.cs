
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class Lab8Tests
{
    // Тест 1. Помилка та незмінність стану
    [Fact]
    public void AddLine_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);

        Order order = service.CreateOrder("C001");

        service.AddLine(
            order.Id,
            "P001",
            "Кабель",
            100m,
            2);

        decimal totalBefore = order.Total;
        int linesBefore = order.Lines.Count;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.AddLine(
                order.Id,
                "P002",
                "Мишка",
                500m,
                -5));

        Order storedOrder = store.GetById(order.Id)!;

        Assert.Equal(totalBefore, storedOrder.Total);
        Assert.Equal(linesBefore, storedOrder.Lines.Count);
    }

    // Тест 2. Подія Changed викликається один раз
    [Fact]
    public void AddLine_RaisesChangedExactlyOnce()
    {
        var service =
            new OrderService(new InMemoryOrderStore());

        Order order = service.CreateOrder("C002");

        var events = new List<OrderChangedEventArgs>();

        service.Changed += (_, e) => events.Add(e);

        service.AddLine(
            order.Id,
            "P003",
            "Клавіатура",
            1500m,
            1);

        Assert.Single(events);

        Assert.Equal(
            OrderChangeKind.LineAdded,
            events[0].Kind);
    }

    // Тест 3. Перевірка LINQ-звіту для різних порогів
    [Theory]
    [InlineData(1000, 2)]
    [InlineData(5000, 1)]
    public void ExpensiveOrders_CountsCorrectly(
        int threshold,
        int expected)
    {
        var orders = TestData.Items();

        var result =
            ReportService.ExpensiveOrders(
                orders,
                threshold);

        Assert.Equal(expected, result.Count);
    }

    // Тест 4. Детермінований топ-N
    [Fact]
    public void TopCustomers_IsDeterministic()
    {
        var orders = TestData.TopCustomersItems();

        var result =
            ReportService.TopCustomersByTotal(
                orders,
                2);

        string[] actual =
            result.Select(x => x.CustomerId).ToArray();

        Assert.Equal(
            new[] { "C-BIG", "C-MID" },
            actual);
    }

    // Тест 5. Скасування асинхронного імпорту
    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var service =
            new OrderService(new InMemoryOrderStore());

        Order order = service.CreateOrder("C003");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(
                new JsonOrderLineImporter(),
                order.Id,
                "any.json",
                cts.Token));
    }

    // Тест 6. Некоректний запис у тимчасовому JSON
    // Інтеграційна перевірка справжнього імпортера
    [Fact]
    public async Task ImportAsync_WithInvalidQuantity_ThrowsDomainException()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"lab8-invalid-{Guid.NewGuid():N}.json");

        try
        {
            string json = """
            [
                {
                    "ProductId": "P999",
                    "Name": "Некоректний товар",
                    "Price": 100,
                    "Quantity": -5
                }
            ]
            """;

            await File.WriteAllTextAsync(path, json);

            var service =
                new OrderService(new InMemoryOrderStore());

            Order order = service.CreateOrder("C004");

            await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(
                    new JsonOrderLineImporter(),
                    order.Id,
                    path));

            Assert.Empty(order.Lines);
            Assert.Equal(0m, order.Total);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // Тест 7. Доменний виняток без файлової системи
    [Fact]
    public async Task ImportAsync_WhenImporterThrowsDomainException_KeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);

        Order order = service.CreateOrder("C005");

        int changedCount = 0;

        service.Changed += (_, e) =>
        {
            if (e.Kind == OrderChangeKind.LineAdded)
            {
                changedCount++;
            }
        };

        var importer = new FakeOrderLineImporter();

        await Assert.ThrowsAsync<DomainException>(
            () => service.ImportAsync(
                importer,
                order.Id,
                "unused.json"));

        Order storedOrder = store.GetById(order.Id)!;

        Assert.Empty(storedOrder.Lines);
        Assert.Equal(0m, storedOrder.Total);
        Assert.Equal(0, changedCount);
    }

    // Тест 8. Перевірка суми замовлення після додавання рядків
    [Fact]
    public void AddLines_CalculatesOrderTotalCorrectly()
    {
        var service =
            new OrderService(new InMemoryOrderStore());

        Order order = service.CreateOrder("C006");

        service.AddLine(
            order.Id,
            "P001",
            "Клавіатура",
            1500m,
            2);

        service.AddLine(
            order.Id,
            "P002",
            "Мишка",
            500m,
            3);

        Assert.Equal(4500m, order.Total);
        Assert.Equal(2, order.Lines.Count);
    }
}

// Підставний імпортер для ізольованого unit-тесту
internal sealed class FakeOrderLineImporter : IOrderLineImporter
{
    public Task<IReadOnlyList<OrderLine>> LoadAsync(
        string path,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromException<IReadOnlyList<OrderLine>>(
            new DomainException("Некоректні дані імпорту."));
    }
}
