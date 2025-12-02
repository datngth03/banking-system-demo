namespace BankingSystem.Application.Exceptions;

public class UnauthorizedException : BankingApplicationException
{
    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
