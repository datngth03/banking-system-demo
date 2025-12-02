namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Accounts;

public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.AccountType)
            .NotEmpty().WithMessage("Account type is required")
            .Must(BeValidAccountType).WithMessage("Invalid account type");

        RuleFor(x => x.IBAN)
            .Matches(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$").When(x => !string.IsNullOrEmpty(x.IBAN))
            .WithMessage("IBAN must be valid");

        RuleFor(x => x.BIC)
            .Matches(@"^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$").When(x => !string.IsNullOrEmpty(x.BIC))
            .WithMessage("BIC must be valid");
    }

    private static bool BeValidAccountType(string accountType)
    {
        var validTypes = new[] { "Checking", "Savings", "MoneyMarket", "CertificateOfDeposit" };
        return validTypes.Contains(accountType);
    }
}
