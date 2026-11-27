using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;
using ServiceDesk.Api.Payments;

namespace ServiceDesk.Api.Controllers;

// Razorpay calls this. It can't send our X-Api-Key, so the signature is the authentication.
[ApiController]
[Route("webhooks")]
[SkipApiKey]
public class WebhooksController(
    AppDbContext db, IOptions<RazorpayOptions> razorpay, ILogger<WebhooksController> logger) : ControllerBase
{
    // POST /webhooks/razorpay
    [HttpPost("razorpay")]
    public async Task<IActionResult> Razorpay(CancellationToken ct)
    {
        // 1. Read the RAW body: the signature covers these exact bytes, so never parse first.
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, ct);
        var rawBody = buffer.ToArray();

        // 2. Verify the signature before trusting anything in the body.
        var secret = razorpay.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogError("Razorpay webhook secret is not configured");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Webhook not configured");
        }
        if (!RazorpaySignature.IsValid(rawBody, Request.Headers["X-Razorpay-Signature"], secret))
        {
            logger.LogWarning("Rejected a Razorpay webhook with an invalid signature");
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid signature");
        }

        // 3. Idempotency: Razorpay retries, and every delivery of one event has the same id.
        var eventId = Request.Headers["X-Razorpay-Event-Id"].ToString();
        if (string.IsNullOrWhiteSpace(eventId))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Missing X-Razorpay-Event-Id");

        if (await db.WebhookEvents.AnyAsync(e => e.EventId == eventId, ct))
        {
            logger.LogInformation("Razorpay event {EventId} already processed; ignoring the repeat", eventId);
            return Ok();
        }

        // 4. Apply the event. The event row and the booking change are saved together.
        using var json = JsonDocument.Parse(rawBody);
        var root = json.RootElement;
        var eventType = root.GetProperty("event").GetString() ?? "unknown";

        db.WebhookEvents.Add(new WebhookEvent { EventId = eventId, EventType = eventType });
        var outcome = eventType == "payment_link.paid"
            ? await ApplyPaymentLinkPaidAsync(root, ct)
            : "ignored (event type not handled)";

        try
        {
            await db.SaveChangesAsync(ct);      // one transaction: both rows or neither
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two deliveries of the same event arrived at the same moment; the other one won.
            logger.LogInformation("Razorpay event {EventId} was handled by a parallel delivery", eventId);
            return Ok();
        }

        logger.LogInformation("Razorpay event {EventId} ({EventType}): {Outcome}", eventId, eventType, outcome);
        return Ok();
    }

    private async Task<string> ApplyPaymentLinkPaidAsync(JsonElement root, CancellationToken ct)
    {
        var payload = root.GetProperty("payload");
        var link = payload.GetProperty("payment_link").GetProperty("entity");
        var linkId = link.GetProperty("id").GetString();
        var amountPaid = link.GetProperty("amount_paid").GetInt64();                   // paise
        var paymentId = payload.TryGetProperty("payment", out var payment)
            ? payment.GetProperty("entity").GetProperty("id").GetString()
            : null;

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.PaymentLinkId == linkId, ct);
        if (booking is null)
            return $"no booking for payment link {linkId}";
        if (booking.PaymentStatus == PaymentStatus.Paid)
            return $"booking {booking.Id} was already paid";                       // second safety net
        if (amountPaid != (long)Math.Round(booking.Amount * 100m))
            return $"amount mismatch for booking {booking.Id}: paid {amountPaid} paise, expected {booking.Amount * 100m:0}";

        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaymentId = paymentId;
        booking.PaidAt = DateTime.UtcNow;
        return $"booking {booking.Id} marked Paid";
    }
}