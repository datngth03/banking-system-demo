namespace BankingSystem.Application.Validators;

using BankingSystem.Application.Commands.Auth;
using FluentValidation;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        // Use PasswordComplexityValidator for enhanced password security
        RuleFor(x => x.NewPassword)
            .SetValidator(new PasswordComplexityValidator())
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from current password");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.NewPassword).WithMessage("Confirm password must match new password");
    }
}
