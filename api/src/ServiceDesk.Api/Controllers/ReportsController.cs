using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;
using Dapper;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    // GET /api/reports/daily?date=2026-11-25   (no date = yesterday, India time)
    [HttpGet("daily")]
    public async Task<ActionResult<DailyReport>> Daily([FromQuery] DateOnly? date)
    {
        var today = IndiaTime.Today();
        var day = date ?? today.AddDays(-1);
        if (day > today)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid date", detail: "The report date cannot be in the future.");

        var (from, to) = IndiaTime.DayToUtcRange(day);
        var leads = db.Leads.AsNoTracking().Where(l => l.CreatedAt >= from && l.CreatedAt < to);
        var bookings = db.Bookings.AsNoTracking().Where(b => b.CreatedAt >= from && b.CreatedAt < to);

        var bySource = await leads
            .GroupBy(l => l.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Source, x => x.Count);

        return Ok(new DailyReport(
            Date: day,
            TimeZone: IndiaTime.ZoneName,
            NewLeads: bySource.Values.Sum(),
            LeadsBySource: bySource,
            NewCustomers: await db.Customers.CountAsync(c => c.CreatedAt >= from && c.CreatedAt < to),
            NewBookings: await bookings.CountAsync(),
            PaidBookings: await bookings.CountAsync(b => b.PaymentStatus == PaymentStatus.Paid),
            Revenue: await bookings.Where(b => b.PaymentStatus == PaymentStatus.Paid).SumAsync(b => b.Amount),
            PendingAmount: await bookings.Where(b => b.PaymentStatus == PaymentStatus.Pending).SumAsync(b => b.Amount),
            OpenLeads: await db.Leads.CountAsync(l => l.Status == LeadStatus.New)));
    }

    // GET /api/reports/top-customers?days=30&limit=10
    // Top customers by paid revenue over the last N full days (India time), today excluded.
    // Hand-tuned SQL run with Dapper; served by IX_Bookings_PaymentStatus_StartsAt (index-only scan).
    [HttpGet("top-customers")]
    public async Task<ActionResult<IEnumerable<TopCustomer>>> TopCustomers(
        [FromQuery] int days = 30, [FromQuery] int limit = 10)
    {
        if (days is < 1 or > 365 || limit is < 1 or > 50)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid parameters", detail: "days must be 1–365 and limit 1–50.");

        var today = IndiaTime.Today();
        var (fromUtc, _) = IndiaTime.DayToUtcRange(today.AddDays(-days));
        var (toUtc, _) = IndiaTime.DayToUtcRange(today);   // start of today = end of yesterday

        const string sql = """
            WITH top_paid AS (
                SELECT "CustomerId", sum("Amount") AS revenue, count(*)::int AS paid_bookings
                FROM "Bookings"
                WHERE "PaymentStatus" = 'Paid'
                  AND "StartsAt" >= @FromUtc
                  AND "StartsAt" <  @ToUtc
                GROUP BY "CustomerId"
                ORDER BY revenue DESC, "CustomerId"
                LIMIT @Limit
            )
            SELECT c."Id" AS "CustomerId", c."Name", c."Email",
                   t.revenue AS "Revenue", t.paid_bookings AS "PaidBookings"
            FROM top_paid t
            JOIN "Customers" c ON c."Id" = t."CustomerId"
            ORDER BY t.revenue DESC, t."CustomerId";
            """;

        var connection = db.Database.GetDbConnection();
        var rows = await connection.QueryAsync<TopCustomer>(sql,
            new { FromUtc = fromUtc, ToUtc = toUtc, Limit = limit });
        return Ok(rows);
    }
}