using FluentValidation;
using ServiceDesk.Api.Contracts;

namespace ServiceDesk.Api.Validation;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9 \-]{7,30}$").WithMessage("Phone may contain digits, spaces, '-' and a leading '+'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9 \-]{7,30}$").WithMessage("Phone may contain digits, spaces, '-' and a leading '+'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public class CreateLeadRequestValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Source).MaximumLength(50);
    }
}

public class UpdateLeadStatusRequestValidator : AbstractValidator<UpdateLeadStatusRequest>
{
    public UpdateLeadStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.StartsAt)
            .Must(d => d.ToUniversalTime() > DateTime.UtcNow)
            .WithMessage("Booking time must be in the future.");
    }
}

public class UpdateBookingRequestValidator : AbstractValidator<UpdateBookingRequest>
{
    public UpdateBookingRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.PaymentStatus).IsInEnum();
    }
}