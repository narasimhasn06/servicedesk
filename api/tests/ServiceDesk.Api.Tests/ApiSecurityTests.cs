using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ServiceDesk.Api.Tests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-key-123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");   // skips migrations/seeding (Development only)
        builder.UseSetting("ApiKey", ApiKey);
        builder.UseSetting("ConnectionStrings:ServiceDesk", "Host=localhost;Database=unused");
    }
}

public class ApiSecurityTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Missing_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_api_key_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        var response = await client.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_key_with_invalid_body_returns_400()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        var response = await client.PostAsJsonAsync("/api/customers", new { name = "", email = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/reports/daily");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Report_for_a_future_date_returns_400()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        var response = await client.GetAsync("/api/reports/daily?date=2099-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}