namespace BankingSystem.Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(string message) : base(message)
    {
    }

    public InsufficientFundsException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
