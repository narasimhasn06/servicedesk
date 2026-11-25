using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext db) : ControllerBase
{
    // GET /api/bookings?customerId=2
    [HttpGet]
    public async Task<ActionResult<PagedResult<Booking>>> GetAll(
        [FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = db.Bookings.AsNoTracking();
        if (customerId is not null) query = query.Where(b => b.CustomerId == customerId);
        return Ok(await query.OrderBy(b => b.StartsAt).ToPagedResultAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Booking>> GetById(int id)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<Booking>> Create(CreateBookingRequest request)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId))
        return Problem(statusCode: StatusCodes.Status409Conflict, title: "Customer not found", detail: $"Customer {request.CustomerId} does not exist.");

        var booking = new Booking
        {
            CustomerId = request.CustomerId,
            StartsAt = request.StartsAt.ToUniversalTime(),
            Amount = request.Amount
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookingRequest request)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null) return NotFound();

        booking.StartsAt = request.StartsAt.ToUniversalTime();
        booking.Amount = request.Amount;
        booking.PaymentStatus = request.PaymentStatus;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null) return NotFound();

        db.Bookings.Remove(booking);
        await db.SaveChangesAsync();
        return NoContent();
    }
}