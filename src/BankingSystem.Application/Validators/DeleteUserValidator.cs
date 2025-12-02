namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Users;

public class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required");
    }
}
