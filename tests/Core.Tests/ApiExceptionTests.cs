using System.Net;
using Core;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ApiExceptionTests
{
    [Theory]
    [InlineData(true, HttpStatusCode.Conflict)]
    [InlineData(false, HttpStatusCode.InternalServerError)]
    public async Task Api_Handles_Exceptions_Safely(
        bool domainError,
        HttpStatusCode expectedStatus)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");

                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IOrderStore>(
                        new ThrowingOrderStore(domainError));
                });
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Contains("application/problem+json",
            response.Content.Headers.ContentType?.ToString() ?? "");

        Assert.DoesNotContain("System.Exception", body);
        Assert.DoesNotContain("at Core.", body);
        Assert.DoesNotContain("at Api.", body);
        Assert.DoesNotContain("ThrowingOrderStore", body);
        Assert.DoesNotContain("/Users/", body);

        if (domainError)
        {
            Assert.Contains("Порушення бізнес-правила", body);
        }
        else
        {
            Assert.Contains("Внутрішня помилка сервера", body);
            Assert.DoesNotContain("SECRET_TEST_ERROR", body);
        }
    }

    private sealed class ThrowingOrderStore : IOrderStore
    {
        private readonly bool _domainError;

        public ThrowingOrderStore(bool domainError)
        {
            _domainError = domainError;
        }

        public Task<IReadOnlyList<Order>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            if (_domainError)
            {
                throw new DomainException("SECRET_TEST_ERROR");
            }

            throw new InvalidOperationException("SECRET_TEST_ERROR");
        }

        public Task<Order?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(null);

        public Task AddAsync(
            Order item,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(
            Order item,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> RemoveAsync(
            string id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
