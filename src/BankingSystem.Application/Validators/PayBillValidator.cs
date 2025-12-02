namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Bills;

public class PayBillValidator : AbstractValidator<PayBillCommand>
{
    public PayBillValidator()
    {
        RuleFor(x => x.BillId)
            .NotEmpty().WithMessage("Bill ID is required");

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");
    }
}
