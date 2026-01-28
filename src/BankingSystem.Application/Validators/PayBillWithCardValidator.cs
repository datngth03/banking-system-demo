namespace BankingSystem.Application.Validators;

using BankingSystem.Application.Commands.Payments;
using FluentValidation;

/// <summary>
/// Validator for PayBillWithCardCommand
/// </summary>
public class PayBillWithCardValidator : AbstractValidator<PayBillWithCardCommand>
{
    public PayBillWithCardValidator()
    {
        RuleFor(x => x.BillId)
            .NotEmpty()
            .WithMessage("Bill ID is required");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .WithMessage("Payment method ID is required")
            .Matches(@"^pm_|^src_")
            .WithMessage("Invalid payment method ID format");

        RuleFor(x => x.ReceiptEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.ReceiptEmail))
            .WithMessage("Receipt email must be valid");
    }
}
