using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(300).IsRequired();
            entity.Property(c => c.Phone).HasMaxLength(30);
            entity.HasIndex(c => c.Email).IsUnique();           // no duplicate customer
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.Property(l => l.Message).HasMaxLength(2000).IsRequired();
            entity.Property(l => l.Source).HasMaxLength(50);
            entity.Property(l => l.Status).HasConversion<string>().HasMaxLength(20); // stored as "New", not 0
            entity.HasIndex(l => l.Status);                        // fast ?status=New
            entity.HasIndex(l => l.CreatedAt);                     // fast "newest first"
            entity.HasOne<Customer>().WithMany()
             .HasForeignKey(l => l.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);            // can't orphan a lead
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.Amount).HasPrecision(10, 2);   // money: exact decimals
            entity.Property(b => b.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(b => b.StartsAt);
            entity.HasIndex(b => new { b.PaymentStatus, b.StartsAt })
                  .IncludeProperties(b => new { b.CustomerId, b.Amount });
            entity.HasOne<Customer>().WithMany()
             .HasForeignKey(b => b.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
            entity.Property(b => b.PaymentLinkId).HasMaxLength(50);
            entity.Property(b => b.PaymentLinkUrl).HasMaxLength(200);
            entity.Property(b => b.PaymentId).HasMaxLength(50);
            entity.HasIndex(b => b.PaymentLinkId).IsUnique();     // the webhook finds the booking by link id
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);                        // duplicate event id = insert fails
            entity.Property(e => e.EventId).HasMaxLength(100);
            entity.Property(e => e.EventType).HasMaxLength(100);
        });
    }
}