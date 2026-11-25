using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext db) : ControllerBase
{
    // GET /api/bookings?customerId=2
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Booking>>> GetAll([FromQuery] int? customerId)
    {
        var query = db.Bookings.AsNoTracking();
        if (customerId is not null) query = query.Where(b => b.CustomerId == customerId);
        return Ok(await query.OrderBy(b => b.StartsAt).ToListAsync());
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
            return BadRequest($"Customer {request.CustomerId} does not exist.");
        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

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