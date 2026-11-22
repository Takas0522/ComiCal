namespace ComiCal.Batch.Hosting;

public static class ExitCodes
{
    public const int Success = 0;
    public const int Failure = 1;
    public const int PreflightFailure = 2;
    public const int Timeout = 3;
    public const int AlreadyRunning = 4;

    public static bool IsFailure(int code) => code is >= Failure and <= Timeout;

    public static string Summary(int code) => code switch
    {
        Success => "Batch finished successfully.",
        PreflightFailure => "Batch preflight failed or command arguments are invalid.",
        Timeout => "Batch timed out.",
        AlreadyRunning => "Another batch instance is running.",
        _ => "Batch execution failed."
    };
}
