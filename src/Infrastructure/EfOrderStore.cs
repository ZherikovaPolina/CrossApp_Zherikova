using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class EfOrderStore(AppDbContext db) : IOrderStore
{
    private readonly AppDbContext _db = db;

    // Отримання всіх замовлень
    public async Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .ToListAsync(cancellationToken);

        return rows
            .Select(ToDomain)
            .ToList()
            .AsReadOnly();
    }

    // Отримання замовлення за ID
    public async Task<Order?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        return row is null
            ? null
            : ToDomain(row);
    }
// Додавання замовлення
public async Task AddAsync(
    Order item,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(item);

    bool exists = await _db.Orders
        .AnyAsync(
            x => x.Id == item.Id,
            cancellationToken);

    if (exists)
    {
        throw new DomainException(
            $"Замовлення з id={item.Id} уже існує.");
    }

    _db.Orders.Add(ToRow(item));

    await SaveChangesWithConflictHandlingAsync(
        cancellationToken);
}

// Збереження змін з обробкою конфліктів унікальності
private async Task SaveChangesWithConflictHandlingAsync(
    CancellationToken cancellationToken)
{
    try
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex)
        when (ex.InnerException is SqliteException sqliteException
            && sqliteException.SqliteErrorCode == 19
            && (sqliteException.SqliteExtendedErrorCode == 1555
                || sqliteException.SqliteExtendedErrorCode == 2067))
    {
        throw new DomainException(
            "Запис із таким унікальним ідентифікатором уже існує.",
            ex);
    }
}

    // Оновлення замовлення
    public async Task UpdateAsync(
        Order item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existing = await _db.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(
                x => x.Id == item.Id,
                cancellationToken);

        if (existing is null)
        {
            _db.Orders.Add(ToRow(item));
        }
        else
        {
            existing.CustomerId = item.CustomerId;
            existing.CreatedAt = item.CreatedAt;
            existing.Status = item.Status;

            _db.OrderLines.RemoveRange(existing.Lines);
            existing.Lines.Clear();

            existing.Lines.AddRange(
                item.Lines.Select((line, position) =>
                    ToLineRow(line, existing.Id, position)));
        }

        await SaveChangesWithConflictHandlingAsync(
    cancellationToken);
    }

    // Видалення замовлення
    public async Task<bool> RemoveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Orders
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (row is null)
            return false;

        _db.Orders.Remove(row);

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    // Перетворення persistence-моделі на доменну
    private static Order ToDomain(OrderRow row)
    {
        var lines = row.Lines
            .OrderBy(x => x.Position)
            .Select(x => new OrderLineDto(
                x.ProductId,
                x.Name,
                x.Price,
                x.Quantity))
            .ToList()
            .AsReadOnly();

        var dto = new OrderDto(
            row.Id,
            row.CustomerId,
            row.Status,
            lines,
            row.CreatedAt);

        return Order.FromDto(dto);
    }

    // Перетворення доменної моделі на persistence-модель
    private static OrderRow ToRow(Order order)
    {
        return new OrderRow
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt,
            Status = order.Status,

            Lines = order.Lines
                .Select((line, position) =>
                    ToLineRow(line, order.Id, position))
                .ToList()
        };
    }

    // Перетворення рядка замовлення
    private static OrderLineRow ToLineRow(
        OrderLine line,
        string orderId,
        int position)
    {
        return new OrderLineRow
        {
            OrderId = orderId,
            ProductId = line.ProductId,
            Name = line.Name,
            Price = line.Price,
            Quantity = line.Quantity,
            Position = position
        };
    }
}