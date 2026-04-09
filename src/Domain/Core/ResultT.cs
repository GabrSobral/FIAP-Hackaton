namespace fiap_hackaton.Domain.Core;

public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(bool isSuccess, T value, Exception? error)
        : base(isSuccess, error)
    {
        if (!isSuccess && !EqualityComparer<T>.Default.Equals(value, default!))
            throw new InvalidOperationException("A failure result cannot contain a value.");

        Value = value;
    }
}
