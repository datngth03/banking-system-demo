namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Accounts;

public class TransferFundsValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty().WithMessage("From account ID is required");

        RuleFor(x => x.ToAccountId)
            .NotEmpty().WithMessage("To account ID is required")
            .NotEqual(x => x.FromAccountId).WithMessage("Cannot transfer to the same account");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(decimal.MaxValue).WithMessage("Amount is too large");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}
