namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Accounts;

public class CloseAccountValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");
    }
}
