using System;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Data;

// Temporary storage for Day 2. Replaced by EF Core + PostgreSQL on Day 3.
public class InMemoryStore
{
    private int _customerId, _leadId, _bookingId;

    public List<Customer> Customers { get; } = [];
    public List<Lead> Leads { get; } = [];
    public List<Booking> Bookings { get; } = [];

    public int NextCustomerId() => Interlocked.Increment(ref _customerId);
    public int NextLeadId() => Interlocked.Increment(ref _leadId);
    public int NextBookingId() => Interlocked.Increment(ref _bookingId);

    public InMemoryStore()
    {
        var priya = new Customer { Id = NextCustomerId(), Name = "Priya Sharma", Email = "priya.sharma@example.com", Phone ="+91 90000 00001" };
        var arjun = new Customer { Id = NextCustomerId(), Name = "Arjun Rao", Email = "arjun.rao@example.com" };

        Customers.AddRange([priya, arjun]);

        Leads.Add(new Lead { Id = NextLeadId(), CustomerId = priya.Id, Message = "Interested in 1:1 carrer coaching, 4 sessions."});
        Bookings.Add(new Booking { Id = NextBookingId(), CustomerId = arjun.Id, StartAt = DateTime.UtcNow.AddDays(3), Amount = 2500m });  
    }
}
