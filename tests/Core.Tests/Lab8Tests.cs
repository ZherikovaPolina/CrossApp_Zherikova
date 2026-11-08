
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Storage;
using System.Runtime.CompilerServices;

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

    // Тест 3. Перевірка LINQ-звіту
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

    // Тест 6. Некоректний запис у JSON
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
                File.Delete(path);
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
                changedCount++;
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

    // Тест 8. Перевірка суми замовлення
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

    // Тест 9. Потоковий імпорт через IAsyncEnumerable<OrderLine>
    [Fact]
    public async Task ImportStreamAsync_ImportsLinesAndRaisesEvents()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"lab8-stream-{Guid.NewGuid():N}.json");

        try
        {
            string json = """
            [
                {
                    "ProductId": "P101",
                    "Name": "Keyboard",
                    "Price": 1000,
                    "Quantity": 2
                },
                {
                    "ProductId": "P102",
                    "Name": "Mouse",
                    "Price": 500,
                    "Quantity": 3
                }
            ]
            """;

            await File.WriteAllTextAsync(path, json);

            var store = new InMemoryOrderStore();
            var service = new OrderService(store);
            var importer = new JsonOrderLineImporter();

            Order order = service.CreateOrder("C007");

            int eventCount = 0;

            service.Changed += (_, e) =>
            {
                if (e.Kind == OrderChangeKind.LineAdded)
                    eventCount++;
            };

            int added = await service.ImportStreamAsync(
                importer,
                order.Id,
                path);

            Assert.Equal(2, added);
            Assert.Equal(2, order.Lines.Count);
            Assert.Equal(3500m, order.Total);
            Assert.Equal(2, eventCount);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    // Тест 10. Скасування потокового імпорту
    [Fact]
    public async Task ImportStreamAsync_WhenCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var store = new InMemoryOrderStore();
        var service = new OrderService(store);

        Order order = service.CreateOrder("C008");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportStreamAsync(
                new JsonOrderLineImporter(),
                order.Id,
                "any.json",
                cts.Token));

        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Total);
    }

    // Тест 11. Доводимо, що спрацював саме таймаут
    [Fact]
    public async Task ImportWithTimeoutAsync_WhenTimeoutExpires_ThrowsTimeoutException()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);

        Order order = service.CreateOrder("C009");

        using var userCts = new CancellationTokenSource();

        var importer = new SlowOrderLineImporter();

        TimeoutException exception =
            await Assert.ThrowsAsync<TimeoutException>(
                () => service.ImportWithTimeoutAsync(
                    importer,
                    order.Id,
                    "unused.json",
                    TimeSpan.FromMilliseconds(100),
                    userCts.Token));

        Assert.False(userCts.IsCancellationRequested);

        Assert.Contains(
            "Час імпорту перевищив",
            exception.Message);

        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Total);
    }

    // Тест 12. Скасування користувачем не є таймаутом
    [Fact]
    public async Task ImportWithTimeoutAsync_WhenUserCancels_ThrowsCancellation()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);

        Order order = service.CreateOrder("C010");

        using var userCts = new CancellationTokenSource();

        var importer = new SlowOrderLineImporter();

        userCts.CancelAfter(
            TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportWithTimeoutAsync(
                importer,
                order.Id,
                "unused.json",
                TimeSpan.FromSeconds(5),
                userCts.Token));

        Assert.True(userCts.IsCancellationRequested);

        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Total);
    }

    // Тест 13. Потоковий імпорт через IAsyncEnumerable<Product>
    // Додаткове завдання №1
    [Fact]
    public async Task ProductStreamImporter_ImportsProductsCorrectly()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"lab8-products-{Guid.NewGuid():N}.json");

        try
        {
            string json = """
            [
                {
                    "id": "P001",
                    "name": "Ноутбук",
                    "availableQuantity": 10
                },
                {
                    "id": "P002",
                    "name": "Миша",
                    "availableQuantity": 25
                },
                {
                    "id": "P003",
                    "name": "Клавіатура",
                    "availableQuantity": 15
                }
            ]
            """;

            await File.WriteAllTextAsync(path, json);

            var products = new List<Product>();

            await foreach (Product product in
                ProductStreamImporter.LoadAsync(path))
            {
                products.Add(product);
            }

            Assert.Equal(3, products.Count);

            Assert.Equal("P001", products[0].Id);
            Assert.Equal("Ноутбук", products[0].Name);
            Assert.Equal(10, products[0].AvailableQuantity);

            Assert.Equal("P002", products[1].Id);
            Assert.Equal("Миша", products[1].Name);
            Assert.Equal(25, products[1].AvailableQuantity);

            Assert.Equal("P003", products[2].Id);
            Assert.Equal("Клавіатура", products[2].Name);
            Assert.Equal(15, products[2].AvailableQuantity);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
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

    public async IAsyncEnumerable<OrderLine> LoadStreamAsync(
        string path,
        [EnumeratorCancellation]
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<OrderLine> items =
            await LoadAsync(path, ct);

        foreach (OrderLine item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}

// Повільний імпортер для перевірки зв'язаних токенів
internal sealed class SlowOrderLineImporter : IOrderLineImporter
{
    public async Task<IReadOnlyList<OrderLine>> LoadAsync(
        string path,
        CancellationToken ct = default)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(5),
            ct);

        return Array.Empty<OrderLine>();
    }

    public async IAsyncEnumerable<OrderLine> LoadStreamAsync(
        string path,
        [EnumeratorCancellation]
        CancellationToken ct = default)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(5),
            ct);

        yield break;
    }
}
