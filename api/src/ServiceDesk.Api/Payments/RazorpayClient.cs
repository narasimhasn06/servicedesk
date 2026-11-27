using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ServiceDesk.Api.Payments;

public class RazorpayOptions
{
    public string KeyId { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.razorpay.com/v1/";
}

public record RazorpayPaymentLink(string Id, string ShortUrl);

// Thin client for the one Razorpay API call we need: create a Payment Link.
public class RazorpayClient(HttpClient http, IOptions<RazorpayOptions> options, ILogger<RazorpayClient> logger)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull   // omit "contact" when there is no phone
    };

    public async Task<RazorpayPaymentLink> CreatePaymentLinkAsync(
        int bookingId, decimal amount, string customerName, string customerEmail, string? customerPhone,
        CancellationToken ct = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.KeyId) || string.IsNullOrWhiteSpace(o.KeySecret))
            throw new InvalidOperationException("Razorpay API keys are not configured.");

        var body = new
        {
            amount = (long)Math.Round(amount * 100m),                 // Razorpay wants paise: ₹2,500 → 250000
            currency = "INR",
            accept_partial = false,
            reference_id = $"bk-{bookingId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",   // unique per link, max 40 chars
            description = $"Bright Path Coaching - booking #{bookingId}",
            customer = new
            {
                name = customerName,
                email = customerEmail,
                contact = customerPhone is null ? null
                    : new string(customerPhone.Where(c => char.IsDigit(c) || c == '+').ToArray())
            },
            notify = new { sms = false, email = false },              // we send our own messages
            reminder_enable = false,
            notes = new { booking_id = bookingId.ToString() }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "payment_links")
        {
            Content = JsonContent.Create(body, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.KeyId}:{o.KeySecret}")));

        using var response = await http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Razorpay rejected the payment link for booking {BookingId}: {Status} {Error}",
                bookingId, (int)response.StatusCode, json);
            throw new HttpRequestException($"Razorpay returned {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(json);
        return new RazorpayPaymentLink(
            doc.RootElement.GetProperty("id").GetString()!,
            doc.RootElement.GetProperty("short_url").GetString()!);
    }
}