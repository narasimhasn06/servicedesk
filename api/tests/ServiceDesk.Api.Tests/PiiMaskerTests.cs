using ServiceDesk.Api.Ai;

namespace ServiceDesk.Api.Tests;

public class PiiMaskerTests
{
    [Theory]
    [InlineData("Call me on +91 98765 43210", "Call me on [phone]")]
    [InlineData("Mail priya.sharma@example.com today", "Mail [email] today")]
    [InlineData("My number is 9876543210.", "My number is [phone].")]
    [InlineData("Reach me at (+91) 98765-43210 please", "Reach me at [phone] please")]
    [InlineData("Start on 27-11-2026, budget ₹2,500 for 4 sessions", "Start on 27-11-2026, budget ₹2,500 for 4 sessions")]
    [InlineData("Buy 10,000 Instagram followers", "Buy 10,000 Instagram followers")]
    public void Masks_contact_details_and_keeps_everything_else(string input, string expected) =>
        Assert.Equal(expected, PiiMasker.Mask(input).Text);

    [Fact]
    public void Reports_how_much_was_masked()
    {
        var result = PiiMasker.Mask("a@b.com, c@d.org, 98765 43210");
        Assert.Equal(2, result.EmailsMasked);
        Assert.Equal(1, result.PhonesMasked);
    }
}