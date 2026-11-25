using System;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Contracts;

public record CreateCustomerRequest(string Name, string Email, string? Phone);
public record UpdateCustomerRequest(int Id, string Name, string Email, string? Phone);

public record CreateLeadRequest(int CustomerId, string Message, string? Source);
public record UpdateLeadStatusRequest(LeadStatus Status);

public record CreateBookingRequest(int CustomerId, DateTime StartsAt, decimal Amount);
public record UpdateBookingRequest(DateTime StartsAt, decimal Amount, PaymentStatus PaymentStatus);

public record LeadIntakeRequest(string Name, string Email, string? Phone, string Message, string? Source);
public record LeadIntakeResponse(int LeadId, int CustomerId, bool IsNewCustomer);