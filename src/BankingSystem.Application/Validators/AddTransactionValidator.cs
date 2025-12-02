namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Transactions;

public class AddTransactionValidator : AbstractValidator<AddTransactionCommand>
{
    public AddTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(BeValidTransactionType).WithMessage("Invalid transaction type");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }

    private static bool BeValidTransactionType(string transactionType)
    {
        var validTypes = new[] { "Deposit", "Withdrawal", "Transfer", "BillPayment", "InterestCredit", "Fee", "Refund" };
        return validTypes.Contains(transactionType);
    }
}
