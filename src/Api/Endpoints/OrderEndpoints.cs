using Api.Contracts;
using Core.Abstractions;
using Core.Domain;
using Api.Validation;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        // GET: список замовлень із пагінацією та пошуком
group.MapGet("/", async (
    int? page,
    int? pageSize,
    string? search,
    IOrderStore store,
    CancellationToken ct) =>
{
    int currentPage = page ?? 1;
    int currentPageSize = pageSize ?? 20;

    if (currentPage < 1 || currentPageSize < 1 || currentPageSize > 100)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["pagination"] =
                [
                    "page має бути >= 1, а pageSize — від 1 до 100."
                ]
            });
    }

    var orders = await store.ListAsync(ct);

    var query = orders.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
{
    string term = search.Trim();

    query = query.Where(o =>
        o.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        o.CustomerId.Contains(term, StringComparison.OrdinalIgnoreCase));
}

    var filtered = query.ToList();

    int total = filtered.Count;

    var items = filtered
        .OrderBy(o => o.Id)
        .Skip((int)Math.Min(
            ((long)currentPage - 1) * currentPageSize,
            int.MaxValue))
        .Take(currentPageSize)
        .Select(o => OrderDto.FromDomain(o))
        .ToList();

    return Results.Ok(new
    {
        items,
        total
    });
})
.Produces(StatusCodes.Status200OK)
.ProducesValidationProblem();

        // GET: замовлення за ID
        group.MapGet("/{id}", async (
            string id,
            IOrderStore store,
            CancellationToken ct) =>
        {
            var order = await store.GetByIdAsync(id, ct);

            return order is null
                ? Results.NotFound()
               : Results.Ok(OrderDto.FromDomain(order));
        })
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST: створення замовлення
        group.MapPost("/", async (
            SaveOrderRequest request,
            IOrderStore store,
            CancellationToken ct) =>
        {
            var order = CreateOrder(request);

            await store.AddAsync(order, ct);

            return Results.Created(
    $"/api/orders/{order.Id}",
    OrderDto.FromDomain(order));
        })
        .AddEndpointFilter<ValidationFilter<SaveOrderRequest>>()
.Produces<OrderDto>(StatusCodes.Status201Created)
.ProducesValidationProblem();

        // PUT: оновлення замовлення
        group.MapPut("/{id}", async (
            string id,
            SaveOrderRequest request,
            IOrderStore store,
            CancellationToken ct) =>
        {
            var existing = await store.GetByIdAsync(id, ct);

            if (existing is null)
                return Results.NotFound();

            var replacement = CreateOrder(request);

            var dto = replacement.ToDto() with { Id = id };
            replacement = Order.FromDto(dto);

            await store.UpdateAsync(replacement, ct);

            return Results.Ok(OrderDto.FromDomain(replacement));
        })
.AddEndpointFilter<ValidationFilter<SaveOrderRequest>>()
.Produces<OrderDto>(StatusCodes.Status200OK)
.ProducesValidationProblem()
.Produces(StatusCodes.Status404NotFound);

// POST: підтвердження замовлення
group.MapPost("/{id}/confirm", async (
    string id,
    IOrderStore store,
    CancellationToken ct) =>
{
    var order = await store.GetByIdAsync(id, ct);

    if (order is null)
        return Results.NotFound();

    order.Confirm();

    await store.UpdateAsync(order, ct);

    return Results.Ok(OrderDto.FromDomain(order));
})
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

        // DELETE: видалення замовлення
        group.MapDelete("/{id}", async (
            string id,
            IOrderStore store,
            CancellationToken ct) =>
        {
            bool removed = await store.RemoveAsync(id, ct);

            return removed
                ? Results.NoContent()
                : Results.NotFound();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // GET: загальна сума замовлення
        group.MapGet("/{id}/total", async (
            string id,
            IOrderStore store,
            CancellationToken ct) =>
        {
            var order = await store.GetByIdAsync(id, ct);

            return order is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    orderId = order.Id,
                    total = order.Total
                });
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static Order CreateOrder(SaveOrderRequest request)
    {
        var order = Order.Create(request.Customer!);

        order.SetCreatedAt(request.CreatedAt);

        foreach (var line in request.Lines!)
        {
            order.AddLine(
                line!.ProductId!,
                line.Name!,
                line.Price,
                line.Quantity);
        }

        return order;
    }
}
