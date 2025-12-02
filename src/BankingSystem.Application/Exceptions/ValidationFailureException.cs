namespace BankingSystem.Application.Exceptions;

public class ValidationFailureException : BankingApplicationException
{
    public ValidationFailureException(string message) : base(message)
    {
    }

    public ValidationFailureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
