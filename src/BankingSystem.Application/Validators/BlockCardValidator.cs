namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Cards;

public class BlockCardValidator : AbstractValidator<BlockCardCommand>
{
    public BlockCardValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty().WithMessage("Card ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
