namespace ComiCal.Shared;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    internal Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(true, value, null);
    public static Result<T> Failure<T>(string error) => new(false, default, error);
}
