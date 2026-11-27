using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;
using ServiceDesk.Api.Ai;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController(AppDbContext db, ILogger<LeadsController> logger, LeadTriageService triage) : ControllerBase
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

    // GET /api/leads/with-customer?status=New
    // Each lead with its customer in ONE database query (a JOIN), instead of
    // 1 call for the leads + 1 call per lead for the customer (N+1).
    [HttpGet("with-customer")]
    public async Task<ActionResult<PagedResult<LeadWithCustomer>>> GetWithCustomer(
        [FromQuery] LeadStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var leads = db.Leads.AsNoTracking();
        if (status is not null) leads = leads.Where(l => l.Status == status);

        var query =
            from l in leads
            join c in db.Customers on l.CustomerId equals c.Id
            orderby l.CreatedAt descending
            select new LeadWithCustomer(
                l.Id, l.CreatedAt, l.Message, l.Source, l.Status,
                c.Id, c.Name, c.Email, c.Phone);

        return Ok(await query.ToPagedResultAsync(page, pageSize));
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

    // POST /api/leads/intake — one call for website forms and n8n:
    // finds the customer by email (or creates them) and records the lead, in one transaction.
    [HttpPost("intake")]
    public async Task<ActionResult<LeadIntakeResponse>> Intake(LeadIntakeRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        await using var transaction = await db.Database.BeginTransactionAsync();

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email);
        var isNewCustomer = customer is null;

        if (customer is null)
        {
            customer = new Customer
            {
                Name = request.Name.Trim(),
                Email = email,
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();          // gives the new customer an Id
        }

        var lead = new Lead
        {
            CustomerId = customer.Id,
            Message = request.Message.Trim(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "website-form" : request.Source.Trim()
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();         // both rows saved, or neither

        logger.LogInformation("Intake: lead {LeadId} for customer {CustomerId} (new customer: {IsNewCustomer}) from {Source}",
            lead.Id, customer.Id, isNewCustomer, lead.Source);

        return CreatedAtAction(nameof(GetById), new { id = lead.Id },
            new LeadIntakeResponse(lead.Id, customer.Id, isNewCustomer));
    }

    // POST /api/leads/5/triage — the AI reads ONLY the (masked) message and suggests priority, service,
    // a summary and a draft reply. Always 200: Done, or NeedsHumanReview if the AI couldn't be trusted.
    [HttpPost("{id:int}/triage")]
    public async Task<ActionResult<LeadTriageResponse>> Triage(int id, CancellationToken ct)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null) return NotFound();

        var outcome = await triage.TriageAsync(lead.Message, lead.Id, ct);

        var r = outcome.Result;
        lead.AiStatus = outcome.Status;
        lead.AiPriority = r?.Priority;
        lead.AiService = r?.Service;
        lead.AiSummary = r?.Summary;
        lead.AiDraftReply = r?.DraftReply;
        lead.AiTriagedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "AI triage for lead {LeadId}: {Status} {Priority} after {Attempts} attempt(s) in {Ms} ms; masked {Emails} email(s), {Phones} phone(s)",
            id, outcome.Status, r?.Priority, outcome.Attempts, outcome.DurationMs, outcome.EmailsMasked, outcome.PhonesMasked);

        return Ok(new LeadTriageResponse(lead.Id, outcome.Status, r?.Priority, r?.Service,
            r?.Summary, r?.DraftReply, outcome.Error));
    }
}