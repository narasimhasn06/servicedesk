using System.Text.RegularExpressions;

namespace ServiceDesk.Api.Ai;

public record MaskResult(string Text, int EmailsMasked, int PhonesMasked);

// Removes contact details a customer typed INTO their message before it is sent to an AI model.
// Emails become [email]; any run of 10+ digits (phones, also ID-like numbers) becomes [phone].
// Dates (8 digits at most), prices and years are left alone.
public static partial class PiiMasker
{
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex Email();

    // optional bracket and +, a digit, then digits/spaces/dashes/dots/brackets, ending in a digit
    [GeneratedRegex(@"\(?\+?\d[\d\s().-]{6,}\d")]
    private static partial Regex PhoneLike();

    public static MaskResult Mask(string text)
    {
        var emails = 0;
        var phones = 0;

        var masked = Email().Replace(text, _ => { emails++; return "[email]"; });
        masked = PhoneLike().Replace(masked, m =>
        {
            if (m.Value.Count(char.IsDigit) < 10) return m.Value;   // too short to be a phone number
            phones++;
            return "[phone]";
        });

        return new MaskResult(masked, emails, phones);
    }
}