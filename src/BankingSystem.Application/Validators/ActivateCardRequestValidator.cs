namespace BankingSystem.Application.Validators;

using BankingSystem.Application.DTOs.Cards;
using FluentValidation;

public class ActivateCardRequestValidator : AbstractValidator<ActivateCardRequest>
{
    public ActivateCardRequestValidator()
    {
        RuleFor(x => x.LastFourDigits)
            .NotEmpty()
            .WithMessage("Last four digits are required")
            .Length(4)
            .WithMessage("Must be exactly 4 digits")
            .Matches(@"^\d{4}$")
            .WithMessage("Must contain only numbers");
    }
}
