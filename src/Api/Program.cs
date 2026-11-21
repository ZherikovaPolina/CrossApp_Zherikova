using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Storage;
using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;
using Api.Contracts;
using Api.Validation;
using FluentValidation;
using Api.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<
    IValidator<SaveOrderRequest>,
    SaveOrderRequestValidator>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status ==
            StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title =
                "Внутрішня помилка сервера";
        }
    };
});

builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.Configure<RouteHandlerOptions>(options =>
{
    options.ThrowOnBadRequest = false;
});

builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();

var app = builder.Build();

app.UseExceptionHandler();

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
