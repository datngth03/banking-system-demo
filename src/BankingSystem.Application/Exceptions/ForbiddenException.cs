namespace BankingSystem.Application.Exceptions;

public class ForbiddenException : BankingApplicationException
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
