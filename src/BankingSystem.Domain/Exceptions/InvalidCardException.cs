namespace BankingSystem.Domain.Exceptions;

public class InvalidCardException : DomainException
{
    public InvalidCardException(string message) : base(message)
    {
    }

    public InvalidCardException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
