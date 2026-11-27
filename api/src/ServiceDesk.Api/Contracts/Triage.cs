using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Contracts;

public record LeadTriageResponse(
    int LeadId, AiTriageStatus Status, string? Priority, string? Service,
    string? Summary, string? DraftReply, string? Error);