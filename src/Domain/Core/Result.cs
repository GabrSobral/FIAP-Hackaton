namespace fiap_hackaton.Domain.Core;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Exception? Error { get; }

    protected Result(bool isSuccess, Exception? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("A success result cannot contain an error.");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("A failure result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Failure(Exception error) => new(false, error);
    public static Result<T> Failure<T>(Exception error) => new(false, default!, error);
    public static Result Success() => new(true, null);
    public static Result<T> Success<T>(T value) => new(true, value, null);
}
