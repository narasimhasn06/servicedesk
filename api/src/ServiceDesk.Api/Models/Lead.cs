using System;

namespace ServiceDesk.Api.Models;

public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
    Lost
}
public class Lead
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "website";
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
