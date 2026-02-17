namespace Listed.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot have an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, Error error)
        : base(isSuccess, error)
    {
        if (isSuccess && value is null)
        {
            throw new InvalidOperationException("A successful result must have a value.");
        }

        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static new Result<T> Failure(Error error) => new(false, default, error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error);
    }
}
