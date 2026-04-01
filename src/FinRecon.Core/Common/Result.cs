namespace FinRecon.Core.Common;

public record DomainError(string Code, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public DomainError? Error { get; }

    protected Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(DomainError error) => new(false, error);

    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Fail<T>(DomainError error) => Result<T>.Fail(error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(DomainError error) : base(false, error) { }

    public static Result<T> Ok(T value) => new(value);
    public new static Result<T> Fail(DomainError error) => new(error);
}
