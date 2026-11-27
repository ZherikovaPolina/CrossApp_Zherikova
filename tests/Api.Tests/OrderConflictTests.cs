using System.Net;
using System.Net.Http.Json;
using Core;
using Core.Abstractions;
using Core.Domain;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests;

public class OrderConflictTests
{
    private static Order CreateOrder()
    {
        var order = Order.Create("Conflict Test");

        order.AddLine(
            "P001", "Laptop", 25000m, 1);

        return order;
    }

    // 1. Повторне додавання замовлення з однаковим ID
    [Fact]
    public async Task AddDuplicateOrder_ThrowsDomainException()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db = new AppDbContext(options);

        await db.Database.MigrateAsync();

        var store = new EfOrderStore(db);
        var order = CreateOrder();

        await store.AddAsync(order);

        var exception =
            await Assert.ThrowsAsync<DomainException>(
                () => store.AddAsync(order));

        Assert.Contains("уже існує", exception.Message);

        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.OrderLines.CountAsync());
    }

    // 2. Фактичне порушення PRIMARY KEY у SQLite
    [Fact]
    public async Task SqlitePrimaryKeyConflict_ThrowsDomainException()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db = new AppDbContext(options);

        await db.Database.MigrateAsync();

        var store = new EfOrderStore(db);
        var existing = CreateOrder();

        await store.AddAsync(existing);

        db.ChangeTracker.Clear();

        db.Orders.Add(new OrderRow
        {
            Id = existing.Id,
            CustomerId = existing.CustomerId,
            CreatedAt = existing.CreatedAt,
            Status = existing.Status
        });

        var another = CreateOrder();

        var exception =
            await Assert.ThrowsAsync<DomainException>(
                () => store.AddAsync(another));

        var dbException =
            Assert.IsType<DbUpdateException>(
                exception.InnerException);

        var sqliteException =
            Assert.IsType<SqliteException>(
                dbException.InnerException);

        Assert.Equal(19, sqliteException.SqliteErrorCode);

        Assert.Equal(
            1555,
            sqliteException.SqliteExtendedErrorCode);

        db.ChangeTracker.Clear();

        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.OrderLines.CountAsync());
    }

    // 3. Конфлікт унікальності через API
    [Fact]
    public async Task PostOrder_DuplicateId_Returns409()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"crossapp-conflict-{Guid.NewGuid():N}.db");

        try
        {
            await using var factory =
                CreateFactory(databasePath, forceDuplicate: true);

            using var client = factory.CreateClient();

            var request = CreateRequest();

            var first = await client.PostAsJsonAsync(
                "/api/orders", request);

            Assert.Equal(
                HttpStatusCode.Created,
                first.StatusCode);

            var second = await client.PostAsJsonAsync(
                "/api/orders", request);

            Assert.Equal(
                HttpStatusCode.Conflict,
                second.StatusCode);

            Assert.Contains(
                "application/problem+json",
                second.Content.Headers.ContentType?
                    .ToString() ?? "");

            var body =
                await second.Content.ReadAsStringAsync();

            Assert.Contains(
                "Порушення бізнес-правила", body);

            Assert.DoesNotContain(
                "DbUpdateException", body);

            Assert.DoesNotContain(
                "SqliteException", body);

            using var scope =
                factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(1, await db.OrderLines.CountAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    // 4. Однаковий товар у різних замовленнях дозволено
    [Fact]
    public async Task PostOrders_WithSameProduct_Returns201Twice()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"crossapp-orders-{Guid.NewGuid():N}.db");

        try
        {
            await using var factory =
                CreateFactory(databasePath);

            using var client = factory.CreateClient();

            var request = CreateRequest();

            var first = await client.PostAsJsonAsync(
                "/api/orders", request);

            var second = await client.PostAsJsonAsync(
                "/api/orders", request);

            Assert.Equal(
                HttpStatusCode.Created,
                first.StatusCode);

            Assert.Equal(
                HttpStatusCode.Created,
                second.StatusCode);

            Assert.NotNull(first.Headers.Location);
            Assert.NotNull(second.Headers.Location);

            Assert.NotEqual(
                first.Headers.Location,
                second.Headers.Location);

            using var scope =
                factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.Equal(2, await db.Orders.CountAsync());

            Assert.Equal(
                2,
                await db.OrderLines.CountAsync(
                    x => x.ProductId == "P001"));
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static object CreateRequest() => new
    {
        customer = "Conflict Test",

        createdAt = new DateTimeOffset(
            2026, 11, 27, 12, 0, 0, TimeSpan.Zero),

        lines = new[]
        {
            new
            {
                productId = "P001",
                name = "Laptop",
                price = 25000m,
                quantity = 1
            }
        }
    };

    private static WebApplicationFactory<Program>
        CreateFactory(
            string databasePath,
            bool forceDuplicate = false)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<
                        DbContextOptions<AppDbContext>>();

                    services.RemoveAll<
                        IDbContextOptionsConfiguration<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(
                        options => options.UseSqlite(
                            $"Data Source={databasePath};Pooling=False"));

                    if (forceDuplicate)
                    {
                        services.RemoveAll<IOrderStore>();

                        services.AddScoped<
                            IOrderStore,
                            DuplicateOrderStore>();
                    }
                });
            });
    }

    private static void DeleteDatabase(string databasePath)
    {
        File.Delete(databasePath);
        File.Delete(databasePath + "-shm");
        File.Delete(databasePath + "-wal");
    }

    private sealed class DuplicateOrderStore(
        AppDbContext db) : IOrderStore
    {
        private readonly EfOrderStore _store = new(db);

        public Task<IReadOnlyList<Order>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            return _store.ListAsync(cancellationToken);
        }

        public Task<Order?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _store.GetByIdAsync(
                id, cancellationToken);
        }

        public async Task AddAsync(
            Order item,
            CancellationToken cancellationToken = default)
        {
            var orders = await _store.ListAsync(
                cancellationToken);

            if (orders.Count == 0)
            {
                await _store.AddAsync(
                    item, cancellationToken);

                return;
            }

            var dto = item.ToDto() with
            {
                Id = orders[0].Id
            };

            var duplicate = Order.FromDto(dto);

            await _store.AddAsync(
                duplicate, cancellationToken);
        }

        public Task UpdateAsync(
            Order item,
            CancellationToken cancellationToken = default)
        {
            return _store.UpdateAsync(
                item, cancellationToken);
        }

        public Task<bool> RemoveAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _store.RemoveAsync(
                id, cancellationToken);
        }
    }
}