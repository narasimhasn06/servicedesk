using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;

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
}