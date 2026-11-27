using System;

namespace ServiceDesk.Api.Models;

public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
    Lost
}

public enum AiTriageStatus
{
    Done,               // valid answer saved
    NeedsHumanReview,   // used from Day 12 (answer kept failing validation)
    Failed              // the model's answer didn't fit the schema or our rules
}

public class Lead
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "website";
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // AI triage (Day 11): suggestions only, a person decides
    public string? AiPriority { get; set; }        // hot | warm | cold
    public string? AiService { get; set; }
    public string? AiSummary { get; set; }
    public string? AiDraftReply { get; set; }
    public AiTriageStatus? AiStatus { get; set; }  // null = never triaged
    public DateTime? AiTriagedAt { get; set; }
}
