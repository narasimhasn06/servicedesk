using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Contracts;

// One lead plus the customer details CRM Sync needs, returned in one query (no N+1).
public record LeadWithCustomer(
    int Id,
    DateTime CreatedAt,
    string Message,
    string Source,
    LeadStatus Status,
    int CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone);