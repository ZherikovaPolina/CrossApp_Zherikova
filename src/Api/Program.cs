
using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Storage;
using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.Configure<RouteHandlerOptions>(options =>
{
    options.ThrowOnBadRequest = false;
});

builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        if (exception is DomainException)
        {
            context.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await Results.Problem(
                title: "Порушення бізнес-правила",
                detail: "Операцію неможливо виконати через порушення правил замовлення.",
                statusCode: StatusCodes.Status409Conflict
            ).ExecuteAsync(context);
        }
        else
        {
            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            await Results.Problem(
                title: "Внутрішня помилка сервера",
                statusCode: StatusCodes.Status500InternalServerError
            ).ExecuteAsync(context);
        }
    });
});

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () =>
    Results.Ok(new { status = "ok" }));

app.MapOrders();

app.Run();
public partial class Program { }
