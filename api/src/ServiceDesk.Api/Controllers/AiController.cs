using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Ai;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(AppDbContext db, LeadTriageService triage) : ControllerBase
{
    // POST /api/ai/triage-preview — triage a text WITHOUT saving a lead (evaluation, prompt testing)
    [HttpPost("triage-preview")]
    public async Task<ActionResult<TriagePreviewResponse>> Preview(TriagePreviewRequest request, CancellationToken ct)
    {
        var o = await triage.TriageAsync(request.Message, leadId: null, ct);
        return Ok(new TriagePreviewResponse(o.Status, o.Result?.Priority, o.Result?.Service, o.Result?.Summary,
            o.Result?.DraftReply, o.Error, o.Attempts, LeadTriagePrompt.Version,
            o.EmailsMasked, o.PhonesMasked, o.DurationMs));
    }

    // GET /api/ai/usage?days=7 — calls, outcomes, tokens and latency of every AI attempt
    [HttpGet("usage")]
    public async Task<ActionResult<AiUsageReport>> Usage([FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (days is < 1 or > 90)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid parameters", detail: "days must be 1–90.");

        var since = DateTime.UtcNow.AddDays(-days);
        // Loaded into memory: fine for thousands of calls; aggregate in SQL if it ever grows beyond that.
        var calls = await db.AiCalls.AsNoTracking().Where(c => c.CreatedAt >= since).ToListAsync(ct);
        var durations = calls.Select(c => c.DurationMs).Order().ToList();

        return Ok(new AiUsageReport(
            Days: days,
            Calls: calls.Count,
            ByOutcome: calls.GroupBy(c => c.Outcome).ToDictionary(g => g.Key, g => g.Count()),
            InputTokens: calls.Sum(c => (long)(c.InputTokens ?? 0)),
            OutputTokens: calls.Sum(c => (long)(c.OutputTokens ?? 0)),
            AvgDurationMs: durations.Count == 0 ? 0 : (int)durations.Average(),
            P95DurationMs: durations.Count == 0 ? 0 : durations[(int)Math.Ceiling(durations.Count * 0.95) - 1],
            Models: calls.Select(c => c.Model).Distinct().ToList(),
            PromptVersions: calls.Select(c => c.PromptVersion).Distinct().ToList()));
    }
}