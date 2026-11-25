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
}
