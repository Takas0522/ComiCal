using System.Globalization;

namespace ComiCal.Batch.Hosting;

public enum BatchCommandKind { Run, Status, Preflight }

public sealed record BatchCommand(BatchCommandKind Kind, bool Force = false, DateOnly? Date = null)
{
    public const string Usage = "Usage: ComiCal.Batch run [--force] | status [--date yyyy-MM-dd] | preflight";

    public static BatchCommand? Parse(string[] args) => args switch
    {
        [] or ["run"] => new(BatchCommandKind.Run),
        ["run", "--force"] => new(BatchCommandKind.Run, Force: true),
        ["status"] => new(BatchCommandKind.Status),
        ["status", "--date", var value] when DateOnly.TryParseExact(value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            => new(BatchCommandKind.Status, Date: date),
        ["preflight"] => new(BatchCommandKind.Preflight),
        _ => null
    };
}
