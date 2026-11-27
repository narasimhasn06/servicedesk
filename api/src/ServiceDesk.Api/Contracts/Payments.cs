namespace ServiceDesk.Api.Contracts;

public record PaymentLinkResponse(int BookingId, string PaymentLinkId, string PaymentLinkUrl);