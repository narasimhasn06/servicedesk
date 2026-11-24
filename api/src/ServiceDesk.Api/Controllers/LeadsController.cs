using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController(InMemoryStore store) : ControllerBase
    {
        // GET /api/leads?status=New
        [HttpGet]
        public ActionResult<IEnumerable<Lead>> GetAll([FromQuery] LeadStatus? status)
        {
            var leads = status is null ? store.Leads : store.Leads.Where(l => l.Status == status);
            return Ok(leads.OrderByDescending(l => l.CreatedAt));
        }

        [HttpGet("{id:int}")]
        public ActionResult<Lead> GetById(int id)
        {
            var lead = store.Leads.FirstOrDefault(l => l.Id == id);
            return lead is null ? NotFound() : Ok(lead);
        }

        [HttpPost]
        public ActionResult<Lead> Create(CreateLeadRequest request)
        {
            if (store.Customers.All(c => c.Id != request.CustomerId))
                return BadRequest($"Customer with ID {request.CustomerId} does not exist.");

            var lead = new Lead
            {
                Id = store.NextLeadId(),
                CustomerId = request.CustomerId,
                Message = request.Message,
                Source = request.Source ?? "website"
            };

            store.Leads.Add(lead);
            return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead);
        }

        // PATCH /api/leads/5/status { "status": "Contacted" }
        [HttpPatch("{id:int}/status")]
        public IActionResult UpdateStatus(int id, UpdateLeadStatusRequest request)
        {
            var lead = store.Leads.FirstOrDefault(l => l.Id == id);
            if (lead is null) return NotFound();

            lead.Status = request.Status;
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var lead = store.Leads.FirstOrDefault(l => l.Id == id);
            if (lead is null) return NotFound();

            store.Leads.Remove(lead);
            return NoContent();
        }

    }
}
