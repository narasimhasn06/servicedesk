using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController(InMemoryStore store) : ControllerBase
    {
        // GET /api/bookings?customerId=2
        [HttpGet]
        public ActionResult<IEnumerable<Booking>> GetAll([FromQuery] int? customerId)
        {
            var bookings = customerId is null ? store.Bookings : store.Bookings.Where(b => b.CustomerId == customerId);
            return Ok(bookings.OrderBy(b => b.StartAt));
        }

        [HttpGet("{id:int}")]
        public ActionResult<Booking> GetById(int id)
        {
            var booking = store.Bookings.FirstOrDefault(b => b.Id == id);
            return booking is null ? NotFound() : Ok(booking);
        }

        [HttpPost]
        public ActionResult<Booking> Create(CreateBookingRequest request)
        {
            if (store.Customers.All(c => c.Id != request.CustomerId))
                return BadRequest($"Customer with ID {request.CustomerId} does not exist.");

            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            var booking = new Booking
            {
                Id = store.NextBookingId(),
                CustomerId = request.CustomerId,
                StartAt = request.StartAt,
                Amount = request.Amount
            };

            store.Bookings.Add(booking);
            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, UpdateBookingRequest request)
        {
            var booking = store.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking is null) return NotFound();

            booking.StartAt = request.StartAt;
            booking.Amount = request.Amount;
            booking.PaymentStatus = request.PaymentStatus;

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var booking = store.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking is null) return NotFound();

            store.Bookings.Remove(booking);
            return NoContent();
        }
    }
}
