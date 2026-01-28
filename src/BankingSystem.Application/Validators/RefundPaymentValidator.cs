namespace BankingSystem.Application.Validators;

using BankingSystem.Application.Commands.Payments;
using FluentValidation;

/// <summary>
/// Validator for RefundPaymentCommand
/// </summary>
public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Refund amount must be greater than 0");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => x.Reason != null)
            .WithMessage("Refund reason cannot exceed 500 characters");
    }
}
