using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests;

public class OrderApiTests
{
    [Fact]
    public async Task PostOrder_ThenGetByLocation_Returns201And200()
    {
        await using var factory = new WebApplicationFactory<Program>();

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
}
