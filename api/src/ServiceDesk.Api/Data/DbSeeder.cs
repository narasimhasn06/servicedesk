using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;   // already seeded

        string[] names =
        [
            "Priya Sharma", "Arjun Rao", "Meera Iyer", "Rahul Verma", "Ananya Gupta",
            "Karthik Nair", "Sneha Patil", "Vikram Singh", "Divya Menon", "Rohan Desai",
            "Kavya Reddy", "Aditya Joshi", "Pooja Kulkarni", "Siddharth Bose", "Nisha Pillai",
            "Harsha Gowda", "Lakshmi Krishnan", "Manoj Hegde", "Ritu Agarwal", "Suresh Shetty"
        ];

        var customers = names.Select((name, i) => new Customer
        {
            Name = name,
            Email = $"{name.Split(' ')[0].ToLowerInvariant()}.{i + 1}@example.com",
            Phone = $"+91 90000 {i + 1:00000}",
            CreatedAt = DateTime.UtcNow.AddDays(-60 + i)
        }).ToList();

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();                  // customers now have Ids

        string[] sources = ["website", "instagram", "referral", "google"];
        string[] messages =
        [
            "Interested in 1:1 career coaching, 4 sessions.",
            "Looking for help with work-life balance.",
            "Want relationship coaching for couples.",
            "Need guidance on a job switch in IT.",
            "Asking about group workshop pricing."
        ];

        db.Leads.AddRange(customers.Select((c, i) => new Lead
        {
            CustomerId = c.Id,
            Message = messages[i % messages.Length],
            Source = sources[i % sources.Length],
            Status = (LeadStatus)(i % 5),
            CreatedAt = DateTime.UtcNow.AddDays(-(i % 30))
        }));

        db.Bookings.AddRange(customers.Take(10).Select((c, i) => new Booking
        {
            CustomerId = c.Id,
            StartsAt = DateTime.UtcNow.Date.AddDays(i - 3).AddHours(10),
            Amount = 1500m + i * 250m,
            PaymentStatus = i % 3 == 0 ? PaymentStatus.Pending : PaymentStatus.Paid
        }));

        await db.SaveChangesAsync();
    }
}