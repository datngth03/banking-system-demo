namespace BankingSystem.Application.Common;

public class Result<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IList<string> Errors { get; set; } = new List<string>();

    public static Result<T> Success(T data, string? message = null)
    {
        return new Result<T>
        {
            Succeeded = true,
            Data = data,
            Message = message
        };
    }

    public static Result<T> Failure(string message, IList<string>? errors = null)
    {
        return new Result<T>
        {
            Succeeded = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static Result<T> Failure(IList<string> errors)
    {
        return new Result<T>
        {
            Succeeded = false,
            Errors = errors
        };
    }
}

public class Result
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public IList<string> Errors { get; set; } = new List<string>();

    public static Result Success(string? message = null)
    {
        return new Result
        {
            Succeeded = true,
            Message = message
        };
    }

    public static Result Failure(string message, IList<string>? errors = null)
    {
        return new Result
        {
            Succeeded = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static Result Failure(IList<string> errors)
    {
        return new Result
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
