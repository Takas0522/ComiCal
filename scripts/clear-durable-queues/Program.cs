using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Queues;

static string? TryGetHubNameFromHostJson(string hostJsonPath)
{
    if (!File.Exists(hostJsonPath))
    {
        return null;
    }

    using var stream = File.OpenRead(hostJsonPath);
    using var doc = JsonDocument.Parse(stream);

    if (!doc.RootElement.TryGetProperty("extensions", out var extensions))
    {
        return null;
    }

    if (!extensions.TryGetProperty("durableTask", out var durableTask))
    {
        return null;
    }

    if (!durableTask.TryGetProperty("hubName", out var hubNameElement))
    {
        return null;
    }

    return hubNameElement.GetString();
}

static string? TryGetAzureWebJobsStorageFromLocalSettings(string localSettingsPath)
{
    if (!File.Exists(localSettingsPath))
    {
        return null;
    }

    using var stream = File.OpenRead(localSettingsPath);
    using var doc = JsonDocument.Parse(stream);

    if (!doc.RootElement.TryGetProperty("Values", out var values))
    {
        return null;
    }

    if (!values.TryGetProperty("AzureWebJobsStorage", out var storageElement))
    {
        return null;
    }

    return storageElement.GetString();
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: dotnet run --project scripts/clear-durable-queues -- <functionsRootDir> [--dry-run] [--queues] [--tables]");
    Console.Error.WriteLine("  --queues  : Delete/list Durable queues (default)");
    Console.Error.WriteLine("  --tables  : Delete/list Durable tables (additional)");
    Console.Error.WriteLine("  --dry-run : Only list matches");
    Console.Error.WriteLine("Example (queues): dotnet run --project scripts/clear-durable-queues -- src/batch/ComiCal.Batch");
    Console.Error.WriteLine("Example (tables dry-run): dotnet run --project scripts/clear-durable-queues -- src/batch/ComiCal.Batch --tables --dry-run");
}

var argsList = args.ToList();
var dryRun = argsList.Remove("--dry-run");
var wantQueues = argsList.Remove("--queues");
var wantTables = argsList.Remove("--tables");

if (!wantQueues && !wantTables)
{
    // Back-compat default: queues only
    wantQueues = true;
}

if (argsList.Count < 1)
{
    PrintUsage();
    return 2;
}

var functionsRootDir = argsList[0];
if (!Directory.Exists(functionsRootDir))
{
    Console.Error.WriteLine($"Functions root dir not found: {functionsRootDir}");
    return 2;
}

var hostJsonPath = Path.Combine(functionsRootDir, "host.json");
var localSettingsPath = Path.Combine(functionsRootDir, "local.settings.json");

var hubName = TryGetHubNameFromHostJson(hostJsonPath) ?? "DurableFunctionsHub";
var hubNames = new[] { hubName, $"{hubName}Local" };
var hubNamePrefixes = hubNames
    .Select(n => n.ToLowerInvariant())
    .Distinct()
    .ToArray();

var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
    ?? TryGetAzureWebJobsStorageFromLocalSettings(localSettingsPath);

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("AzureWebJobsStorage is not set and could not be read from local.settings.json");
    return 2;
}

Console.WriteLine($"Target storage: AzureWebJobsStorage (from env/local.settings)");
Console.WriteLine($"Target hubName: {hubName} (prefixes: {string.Join(", ", hubNamePrefixes)})");
Console.WriteLine(dryRun ? "Mode: DRY RUN" : "Mode: DELETE");

var serviceClient = new QueueServiceClient(connectionString);

if (wantQueues)
{
    Console.WriteLine("\n== Queues ==");

    // Durable Functions queues are created using the hub name as a prefix (lowercase).
    // We delete ONLY queues that start with the hub name prefix to avoid touching unrelated queues.
    var deletedQueues = 0;
    var matchedQueues = 0;

    foreach (var prefix in hubNamePrefixes)
    {
        await foreach (var queueItem in serviceClient.GetQueuesAsync(prefix: prefix))
        {
            matchedQueues++;
            var queueName = queueItem.Name;
            Console.WriteLine($"- match: {queueName}");

            if (dryRun)
            {
                continue;
            }

            var queueClient = serviceClient.GetQueueClient(queueName);
            var response = await queueClient.DeleteIfExistsAsync();
            if (response.Value)
            {
                deletedQueues++;
            }
        }
    }

    Console.WriteLine($"Matched queues: {matchedQueues}");
    Console.WriteLine(dryRun ? "Deleted queues: (dry-run)" : $"Deleted queues: {deletedQueues}");
}

if (wantTables)
{
    Console.WriteLine("\n== Tables ==");

    // Durable Functions tables are named using the hub name (often with suffixes like Instances/History).
    // We'll match by prefix (case-insensitive) to the same hubName prefixes used for queues.
    var tableServiceClient = new TableServiceClient(connectionString);

    var deletedTables = 0;
    var matchedTables = 0;
    await foreach (var tableItem in tableServiceClient.QueryAsync())
    {
        var tableName = tableItem.Name;
        var tableNameLower = tableName.ToLowerInvariant();

        if (!hubNamePrefixes.Any(p => tableNameLower.StartsWith(p, StringComparison.Ordinal)))
        {
            continue;
        }

        matchedTables++;
        Console.WriteLine($"- match: {tableName}");

        if (dryRun)
        {
            continue;
        }

        await tableServiceClient.DeleteTableAsync(tableName);
        deletedTables++;
    }

    Console.WriteLine($"Matched tables: {matchedTables}");
    Console.WriteLine(dryRun ? "Deleted tables: (dry-run)" : $"Deleted tables: {deletedTables}");
}

return 0;
