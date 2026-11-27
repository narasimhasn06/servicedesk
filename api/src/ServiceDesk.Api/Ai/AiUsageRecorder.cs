using ServiceDesk.Api.Data;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Ai;

public interface IAiUsageRecorder
{
    Task RecordAsync(AiCall call, CancellationToken ct);
}

public class EfAiUsageRecorder(AppDbContext db) : IAiUsageRecorder
{
    public async Task RecordAsync(AiCall call, CancellationToken ct)
    {
        db.AiCalls.Add(call);
        await db.SaveChangesAsync(ct);
    }
}