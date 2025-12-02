namespace BankingSystem.Application.Exceptions;

/// <summary>
/// Base exception for application layer errors
/// </summary>
public class BankingApplicationException : Exception
{
    public BankingApplicationException(string message) : base(message)
    {
    }

    public BankingApplicationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
