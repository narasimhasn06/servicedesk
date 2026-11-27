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
        builder.UseSetting("Razorpay:WebhookSecret", "test-webhook-secret");
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

    [Fact]
    public async Task Top_customers_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/reports/top-customers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Top_customers_with_invalid_days_returns_400()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        var response = await client.GetAsync("/api/reports/top-customers?days=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Leads_with_customer_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/leads/with-customer");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Payment_link_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync("/api/bookings/1/payment-link", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_with_invalid_signature_returns_400_not_401()
    {
        // No X-Api-Key on purpose: the webhook skips the key check, and a forged signature is still refused.
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/razorpay")
        {
            Content = new StringContent("{\"event\":\"payment_link.paid\"}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Razorpay-Signature", "deadbeef");
        request.Headers.Add("X-Razorpay-Event-Id", "evt_test_1");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_without_signature_returns_400()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync("/webhooks/razorpay",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Triage_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync("/api/leads/1/triage", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ai_usage_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/ai/usage");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Triage_preview_without_api_key_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/ai/triage-preview", new { message = "hi" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}