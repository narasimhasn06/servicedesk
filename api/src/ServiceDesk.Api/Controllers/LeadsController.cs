using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController(AppDbContext db, ILogger<LeadsController> logger) : ControllerBase
{
    // GET /api/leads?status=New
    [HttpGet]
    public async Task<ActionResult<PagedResult<Lead>>> GetAll(
        [FromQuery] LeadStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = db.Leads.AsNoTracking();
        if (status is not null) query = query.Where(l => l.Status == status);
        return Ok(await query.OrderByDescending(l => l.CreatedAt).ToPagedResultAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Lead>> GetById(int id)
    {
        var lead = await db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPost]
    public async Task<ActionResult<Lead>> Create(CreateLeadRequest request)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId))
        return Problem(statusCode: StatusCodes.Status404NotFound, title: "Customer not found", detail: $"Customer {request.CustomerId} does not exist.");

        var lead = new Lead
        {
            CustomerId = request.CustomerId,
            Message = request.Message,
            Source = request.Source ?? "website"
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        logger.LogInformation("Lead {LeadId} created for customer {CustomerId} from {Source}",
    lead.Id, lead.CustomerId, lead.Source);
        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateLeadStatusRequest request)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();

        lead.Status = request.Status;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();

        db.Leads.Remove(lead);
        await db.SaveChangesAsync();
        return NoContent();
    }
}