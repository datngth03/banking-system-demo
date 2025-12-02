namespace BankingSystem.Application.Validators;

using BankingSystem.Application.DTOs.Cards;
using FluentValidation;

public class BlockCardRequestValidator : AbstractValidator<BlockCardRequest>
{
    public BlockCardRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
