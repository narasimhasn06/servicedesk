using System.Security.Cryptography;
using System.Text;

namespace ServiceDesk.Api.Payments;

// Razorpay signs every webhook: HMAC-SHA256 of the RAW request body, keyed with the
// webhook secret, sent as lowercase hex in the X-Razorpay-Signature header.
public static class RazorpaySignature
{
    public static bool IsValid(byte[] rawBody, string? signatureHex, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(signatureHex) || string.IsNullOrEmpty(webhookSecret))
            return false;

        byte[] provided;
        try { provided = Convert.FromHexString(signatureHex.Trim()); }
        catch (FormatException) { return false; }            // not hex at all

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), rawBody);

        // Constant-time comparison: an attacker can't learn the signature byte by byte.
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}