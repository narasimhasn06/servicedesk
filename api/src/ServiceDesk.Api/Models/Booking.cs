using System;

namespace ServiceDesk.Api.Models;

public enum PaymentStatus
{
    Pending,
    Paid,
    Refunded,
    Failed
}
public class Booking
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime StartsAt { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Razorpay: set when a payment link is created, then when the payment arrives
    public string? PaymentLinkId { get; set; }     // plink_...
    public string? PaymentLinkUrl { get; set; }    // https://rzp.io/... sent to the customer
    public string? PaymentId { get; set; }         // pay_... from the webhook
    public DateTime? PaidAt { get; set; }
}
