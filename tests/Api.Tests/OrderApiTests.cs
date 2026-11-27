using System.Net;
using System.Net.Http.Json;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests;

public class OrderApiTests
{
    [Fact]
    public async Task PostOrder_ThenGetByLocation_Returns201And200()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"crossapp-test-{Guid.NewGuid():N}.db");

        try
        {
            await using var factory =
                new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseEnvironment("Development");

                        builder.ConfigureTestServices(services =>
                        {
                            services.RemoveAll<
                                DbContextOptions<AppDbContext>>();

                            services.RemoveAll<
                                IDbContextOptionsConfiguration<AppDbContext>>();

                            services.AddDbContext<AppDbContext>(options =>
                                options.UseSqlite(
                                    $"Data Source={databasePath};Pooling=False"));
                        });
                    });

            using var client = factory.CreateClient();

            var request = new
            {
                customer = "Polina",
                createdAt = new DateTimeOffset(
                    2026, 11, 21, 12, 0, 0, TimeSpan.Zero),
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

            var postResponse = await client.PostAsJsonAsync(
                "/api/orders", request);

            Assert.Equal(
                HttpStatusCode.Created,
                postResponse.StatusCode);

            var location = postResponse.Headers.Location;
            Assert.NotNull(location);

            var getResponse = await client.GetAsync(location);

            Assert.Equal(
                HttpStatusCode.OK,
                getResponse.StatusCode);
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete(databasePath + "-shm");
            File.Delete(databasePath + "-wal");
        }
    }
}