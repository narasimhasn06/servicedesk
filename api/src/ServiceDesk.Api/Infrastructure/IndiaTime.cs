// India has one time zone and no daylight saving, so a fixed +05:30 offset is exact
// and behaves the same on Windows, Linux and in Docker (no time-zone database needed).
public static class IndiaTime
{
    public const string ZoneName = "Asia/Kolkata";
    public static readonly TimeSpan Offset = new(5, 30, 0);

    public static DateOnly Today(DateTime? utcNow = null) =>
        DateOnly.FromDateTime((utcNow ?? DateTime.UtcNow) + Offset);

    // One Indian calendar day as a UTC range [from, to)
    public static (DateTime FromUtc, DateTime ToUtc) DayToUtcRange(DateOnly day)
    {
        var fromUtc = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);
        return (fromUtc, fromUtc.AddDays(1));
    }
}