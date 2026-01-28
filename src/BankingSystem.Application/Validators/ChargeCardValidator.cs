namespace BankingSystem.Application.Validators;

using BankingSystem.Application.Commands.Payments;
using BankingSystem.Application.Constants;
using FluentValidation;

/// <summary>
/// Validator for ChargeCardCommand
/// </summary>
public class ChargeCardValidator : AbstractValidator<ChargeCardCommand>
{
    public ChargeCardValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency code is required")
            .Length(3)
            .WithMessage("Currency code must be 3 characters (ISO 4217)");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .WithMessage("Payment method ID is required")
            .Matches(@"^pm_|^src_")
            .WithMessage("Invalid payment method ID format");

        RuleFor(x => x.ReceiptEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.ReceiptEmail))
            .WithMessage("Receipt email must be valid");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 1000 characters");
    }
}
