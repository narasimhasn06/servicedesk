using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Validation;

namespace ServiceDesk.Api.Tests;

public class ValidatorTests
{
    private readonly CreateCustomerRequestValidator _customer = new();
    private readonly CreateBookingRequestValidator _booking = new();

    [Fact]
    public void Valid_customer_passes()
    {
        var result = _customer.Validate(new CreateCustomerRequest("Priya Sharma", "priya@example.com", "+91 90000 00001"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var result = _customer.Validate(new CreateCustomerRequest("Priya", "not-an-email", null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _customer.Validate(new CreateCustomerRequest("", "priya@example.com", null));
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Zero_amount_fails()
    {
        var result = _booking.Validate(new CreateBookingRequest(1, DateTime.UtcNow.AddDays(1), 0));
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Booking_in_the_past_fails()
    {
        var result = _booking.Validate(new CreateBookingRequest(1, DateTime.UtcNow.AddHours(-1), 2500));
        Assert.Contains(result.Errors, e => e.PropertyName == "StartsAt");
    }
}