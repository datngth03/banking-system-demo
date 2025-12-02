using FluentValidation;

namespace BankingSystem.Application.Validators;

/// <summary>
/// Validator for password complexity requirements
/// Ensures passwords meet security standards
/// </summary>
public class PasswordComplexityValidator : AbstractValidator<string>
{
    public PasswordComplexityValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter (A-Z)")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter (a-z)")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one digit (0-9)")
            .Matches(@"[@$!%*?&#^()_\-+=\[\]{}|\\:;""'<>,.?/~`]")
            .WithMessage("Password must contain at least one special character (@, $, !, %, *, etc.)")
            .Must(NotContainCommonPatterns)
            .WithMessage("Password contains common patterns and is not secure. Please choose a stronger password.");
    }

    /// <summary>
    /// Checks if password contains common weak patterns
    /// </summary>
    private bool NotContainCommonPatterns(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // Common weak password patterns
        var commonPatterns = new[]
        {
            "password", "12345", "qwerty", "abc123", "admin", "letmein",
            "welcome", "monkey", "login", "starwars", "dragon", "master",
            "123456", "password123", "admin123", "test", "demo"
        };

        var lowerPassword = password.ToLower();
        return !commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }

    /// <summary>
    /// Validates sequential characters (e.g., "abcd", "1234")
    /// </summary>
    private bool NotContainSequentialCharacters(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 4)
            return true;

        for (int i = 0; i < password.Length - 3; i++)
        {
            if (char.IsLetterOrDigit(password[i]) &&
                char.IsLetterOrDigit(password[i + 1]) &&
                char.IsLetterOrDigit(password[i + 2]) &&
                char.IsLetterOrDigit(password[i + 3]))
            {
                // Check if sequential
                if (password[i] + 1 == password[i + 1] &&
                    password[i] + 2 == password[i + 2] &&
                    password[i] + 3 == password[i + 3])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
