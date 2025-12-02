namespace BankingSystem.Application.Validators;

using BankingSystem.Application.DTOs.Accounts;
using BankingSystem.Application.Constants;
using FluentValidation;

public class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(10000000)
            .WithMessage("Amount cannot exceed 10,000,000");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(50)
            .WithMessage("Reference number cannot exceed 50 characters");
    }
}
