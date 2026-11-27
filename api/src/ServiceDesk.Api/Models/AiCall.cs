namespace ServiceDesk.Api.Models;

// One row per attempt to call the AI model. Content is never stored: only what it cost and how it went.
public class AiCall
{
    public int Id { get; set; }
    public int? LeadId { get; set; }                 // null for previews / evaluation
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public int Attempt { get; set; }                 // 1, or 2 for the retry
    public string Outcome { get; set; } = string.Empty;   // ok | invalid | timeout | provider-error
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}