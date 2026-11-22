namespace ComiCal.Batch.Hosting;

public static class BatchPaths
{
    public static string Root { get; } = Path.Combine(OperatingSystem.IsWindows()
        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
        "ComiCal", "Batch");

    public static string LocalSettings => Path.Combine(Root, "appsettings.Local.json");
    public static string Logs => Path.Combine(Root, "logs");
}
