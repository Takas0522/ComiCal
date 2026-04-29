namespace ComiCal.Shared;

public enum ErrorKind
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    RateLimited = 6,
    Unexpected = 99,
}

public sealed record Error(ErrorKind Kind, string Code, string Message)
{
    public static readonly Error None = new(ErrorKind.None, "none", string.Empty);

    public static Error Validation(string code, string message) => new(ErrorKind.Validation, code, message);

    public static Error NotFound(string code, string message) => new(ErrorKind.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);

    public static Error Unexpected(string code, string message) => new(ErrorKind.Unexpected, code, message);
}
