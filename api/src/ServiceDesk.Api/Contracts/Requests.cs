using System;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Contracts;

public record CreateCustomerRequest(string Name, string Email, string? Phone);
public record UpdateCustomerRequest(int Id, string Name, string Email, string? Phone);

public record CreateLeadRequest(int CustomerId, string Message, string? Source);
public record UpdateLeadStatusRequest(LeadStatus Status);

public record CreateBookingRequest(int CustomerId, DateTime StartAt, decimal Amount);
public record UpdateBookingRequest(DateTime StartAt, decimal Amount, PaymentStatus PaymentStatus);
