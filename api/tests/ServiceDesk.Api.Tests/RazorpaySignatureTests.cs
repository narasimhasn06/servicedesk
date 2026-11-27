using System.Text;
using ServiceDesk.Api.Payments;

namespace ServiceDesk.Api.Tests;

public class RazorpaySignatureTests
{
    private const string Secret = "test-webhook-secret";
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("{\"event\":\"payment_link.paid\"}");

    // HMAC-SHA256(Secret, Body) as hex, computed independently (Python hmac), not by the code under test
    private const string ValidSignature = "5a5a36e2d7644a206e28a317b1eec74bef05c757640b073580477885b309d648";

    [Fact]
    public void Valid_signature_is_accepted() =>
        Assert.True(RazorpaySignature.IsValid(Body, ValidSignature, Secret));

    [Fact]
    public void Changed_body_is_rejected()
    {
        var tampered = Encoding.UTF8.GetBytes("{\"event\":\"payment_link.paid\" }");   // one extra space
        Assert.False(RazorpaySignature.IsValid(tampered, ValidSignature, Secret));
    }

    [Fact]
    public void Wrong_secret_is_rejected() =>
        Assert.False(RazorpaySignature.IsValid(Body, ValidSignature, "some-other-secret"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-hex!")]
    [InlineData("abcd")]
    public void Missing_or_malformed_signature_is_rejected(string? signature) =>
        Assert.False(RazorpaySignature.IsValid(Body, signature, Secret));
}