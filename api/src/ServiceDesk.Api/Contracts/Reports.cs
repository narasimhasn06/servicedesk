namespace ServiceDesk.Api.Contracts;

public record DailyReport(
    DateOnly Date,
    string TimeZone,
    int NewLeads,
    IReadOnlyDictionary<string, int> LeadsBySource,
    int NewCustomers,
    int NewBookings,
    int PaidBookings,
    decimal Revenue,
    decimal PendingAmount,
    int OpenLeads);