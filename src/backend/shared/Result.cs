namespace ComiCal.Shared;

public readonly record struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error Error { get; }

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, ComiCal.Shared.Error.None);

    public static Result<T> Failure(Error error) => new(false, default, error);
}

public readonly record struct Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static readonly Result SuccessResult = new(true, ComiCal.Shared.Error.None);

    public static Result Success() => SuccessResult;

    public static Result Failure(Error error) => new(false, error);
}
