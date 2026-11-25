using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<Customer>>> GetAll(
    [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
    Ok(await db.Customers.AsNoTracking().OrderBy(c => c.Name).ToPagedResultAsync(page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(CreateCustomerRequest request)
    {
        if (await db.Customers.AnyAsync(c => c.Email == request.Email))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate email", detail: $"A customer with email {request.Email} already exists.");

        var customer = new Customer { Name = request.Name, Email = request.Email, Phone = request.Phone };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();                 // the database assigns the Id
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        if (await db.Customers.AnyAsync(c => c.Email == request.Email && c.Id != id))
           return Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate email", detail: $"A customer with email {request.Email} already exists.");

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        var hasHistory = await db.Leads.AnyAsync(l => l.CustomerId == id)
                      || await db.Bookings.AnyAsync(b => b.CustomerId == id);
        if (hasHistory)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Customer has associated records", detail: "This customer has leads or bookings. Delete those first.");

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
        return NoContent();
    }
}