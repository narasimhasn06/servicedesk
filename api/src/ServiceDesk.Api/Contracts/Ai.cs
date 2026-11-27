using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Contracts;

public record TriagePreviewRequest(string Message);

public record TriagePreviewResponse(
    AiTriageStatus Status, string? Priority, string? Service, string? Summary, string? DraftReply,
    string? Error, int Attempts, string PromptVersion, int EmailsMasked, int PhonesMasked, long DurationMs);

public record AiUsageReport(
    int Days, int Calls, IReadOnlyDictionary<string, int> ByOutcome,
    long InputTokens, long OutputTokens, int AvgDurationMs, int P95DurationMs,
    IReadOnlyList<string> Models, IReadOnlyList<string> PromptVersions);