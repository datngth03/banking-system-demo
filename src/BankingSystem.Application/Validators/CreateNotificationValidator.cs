namespace BankingSystem.Application.Validators;

using FluentValidation;
using BankingSystem.Application.Commands.Notifications;

public class CreateNotificationValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters");

        RuleFor(x => x.Type)
            .MaximumLength(50).WithMessage("Type cannot exceed 50 characters");
    }
}
