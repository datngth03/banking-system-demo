namespace BankingSystem.Domain.Exceptions;

public class InvalidAccountException : DomainException
{
    public InvalidAccountException(string message) : base(message)
    {
    }

    public InvalidAccountException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
