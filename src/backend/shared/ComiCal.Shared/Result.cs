namespace ComiCal.Shared;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static Error NotFound(string resource) => new($"{resource}.NotFound", $"{resource} was not found.");
    public static Error AlreadyExists(string resource) => new($"{resource}.AlreadyExists", $"{resource} already exists.");
    public static Error Unauthorized() => new("Unauthorized", "Unauthorized access.");
    public static Error Validation(string message) => new("Validation", message);
    public static Error Conflict(string message) => new("Conflict", message);
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public Error Error { get; }

    internal Result(bool isSuccess, T value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(true, value, Error.None);
    public static Result<T> Failure<T>(Error error) => new(false, default!, error);
}
