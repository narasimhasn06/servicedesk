using ServiceDesk.Api.Infrastructure;

namespace ServiceDesk.Api.Tests;

public class IndiaTimeTests
{
    [Fact]
    public void Indian_day_starts_at_1830_utc_the_evening_before()
    {
        var (from, to) = IndiaTime.DayToUtcRange(new DateOnly(2026, 11, 25));
        Assert.Equal(new DateTime(2026, 11, 24, 18, 30, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2026, 11, 25, 18, 30, 0, DateTimeKind.Utc), to);
        Assert.Equal(DateTimeKind.Utc, from.Kind);
    }

    [Fact]
    public void Late_evening_utc_is_already_the_next_day_in_india()
    {
        var utc = new DateTime(2026, 11, 25, 19, 0, 0, DateTimeKind.Utc);   // 00:30 IST on the 26th
        Assert.Equal(new DateOnly(2026, 11, 26), IndiaTime.Today(utc));
    }
}