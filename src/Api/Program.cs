using Api.Endpoints;
using Core;
using Core.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;
using Api.Contracts;
using Api.Validation;
using FluentValidation;
using Api.ExceptionHandling;
using Microsoft.Data.Sqlite;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

FluentValidation.ValidatorOptions.Global.LanguageManager.Culture =
    new System.Globalization.CultureInfo("uk");

var builder = WebApplication.CreateBuilder(args);

var raw = builder.Configuration.GetConnectionString("AppDb")
    ?? throw new InvalidOperationException(
        "Рядок підключення ConnectionStrings:AppDb не налаштовано.");

var csb = new SqliteConnectionStringBuilder(raw);

if (string.IsNullOrWhiteSpace(csb.DataSource))
{
    throw new InvalidOperationException(
        "Шлях до бази даних SQLite не задано.");
}

if (!Path.IsPathRooted(csb.DataSource))
{
    csb.DataSource = Path.GetFullPath(
        Path.Combine(
            builder.Environment.ContentRootPath,
            csb.DataSource));
}
else
{
    csb.DataSource = Path.GetFullPath(csb.DataSource);
}

Directory.CreateDirectory(
    Path.GetDirectoryName(csb.DataSource)!);

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(csb.ConnectionString));

builder.Services.AddScoped<IOrderStore, EfOrderStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    scope.ServiceProvider
        .GetRequiredService<AppDbContext>()
        .Database.Migrate();
}

app.Logger.LogInformation(
    "SQLite database path: {DatabasePath}",
    csb.DataSource);

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