using Api.Contracts;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        // GET: список усіх замовлень
        group.MapGet("/", async (
            IOrderStore store,
            CancellationToken ct) =>
        {
            var orders = await store.ListAsync(ct);

            return Results.Ok(
                orders.Select(o => o.ToDto()).ToList());
        })
        .Produces<List<OrderDto>>(StatusCodes.Status200OK);

        // GET: замовлення за ID
        group.MapGet("/{id}", async (
            string id,
            IOrderStore store,
            CancellationToken ct) =>
        {
            var order = await store.GetByIdAsync(id, ct);

            return order is null
                ? Results.NotFound()
                : Results.Ok(order.ToDto());
        })
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST: створення замовлення
        group.MapPost("/", async (
            SaveOrderRequest request,
            IOrderStore store,
            CancellationToken ct) =>
        {
            if (!request.TryValidate(out var errors))
                return Results.ValidationProblem(errors);

            var order = CreateOrder(request);

            await store.AddAsync(order, ct);

            return Results.Created(
                $"/api/orders/{order.Id}",
                order.ToDto());
        })
        .Produces<OrderDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT: оновлення замовлення
        group.MapPut("/{id}", async (
            string id,
            SaveOrderRequest request,
            IOrderStore store,
            CancellationToken ct) =>
        {
            if (!request.TryValidate(out var errors))
                return Results.ValidationProblem(errors);

            var existing = await store.GetByIdAsync(id, ct);

            if (existing is null)
                return Results.NotFound();

            var replacement = CreateOrder(request);

            var dto = replacement.ToDto() with { Id = id };
            replacement = Order.FromDto(dto);

            await store.UpdateAsync(replacement, ct);

            return Results.Ok(replacement.ToDto());
        })
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

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
