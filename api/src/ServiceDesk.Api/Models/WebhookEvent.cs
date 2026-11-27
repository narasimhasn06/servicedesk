namespace ServiceDesk.Api.Models;

// One row per webhook event we have processed.
// The primary key is Razorpay's event id, so "process once" is enforced by the database.
public class WebhookEvent
{
    public string EventId { get; set; } = string.Empty;     // X-Razorpay-Event-Id
    public string EventType { get; set; } = string.Empty;   // e.g. payment_link.paid
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}