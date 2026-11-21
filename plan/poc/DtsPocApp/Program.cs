using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

int payloadBytes = 0;
bool validCommand = args switch
{
    ["run"] or ["start-long"] => true,
    ["payload", var size] => int.TryParse(size, out payloadBytes) && payloadBytes > 0,
    ["terminate", var id] => !string.IsNullOrWhiteSpace(id),
    _ => false,
};

if (!validCommand)
{
    Console.Error.WriteLine("Usage: run | payload <bytes> | start-long | terminate <instanceId>");
    return 2;
}

string? connectionString = Environment.GetEnvironmentVariable("DTS_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Set DTS_CONNECTION_STRING before running the PoC.");
    return 2;
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
builder.Services.AddDurableTaskClient(client => client.UseDurableTaskScheduler(connectionString));
builder.Services.AddDurableTaskWorker()
    .AddTasks(tasks =>
    {
        tasks.AddOrchestrator<ChainingOrchestration>();
        tasks.AddOrchestrator<PayloadOrchestration>();
        tasks.AddOrchestrator<LongOrchestration>();
        tasks.AddActivity<IncrementActivity>();
        tasks.AddActivity<PayloadActivity>();
    })
    .UseDurableTaskScheduler(connectionString);

using IHost host = builder.Build();
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
CancellationToken cancellation = deadline.Token;

try
{
    await host.StartAsync(cancellation);
    DurableTaskClient client = host.Services.GetRequiredService<DurableTaskClient>();
    string command = args[0];

    if (command == "terminate")
    {
        string target = args[1];
        OrchestrationMetadata? before = await client.GetInstanceAsync(target, cancellation: cancellation);
        if (before?.RuntimeStatus != OrchestrationRuntimeStatus.Running)
        {
            Console.Error.WriteLine($"InstanceId={target} status before terminate: {before?.RuntimeStatus.ToString() ?? "Missing"}");
            return 1;
        }

        Console.WriteLine($"InstanceId={target} before=Running");
        await client.TerminateInstanceAsync(target, "DTS PoC", cancellation);
        OrchestrationMetadata terminated = await client.WaitForInstanceCompletionAsync(target, cancellation: cancellation);
        Console.WriteLine($"InstanceId={target} status={terminated.RuntimeStatus}");
        return terminated.RuntimeStatus == OrchestrationRuntimeStatus.Terminated ? 0 : 1;
    }

    var (name, input) = command switch
    {
        "run" => (nameof(ChainingOrchestration), 0),
        "payload" => (nameof(PayloadOrchestration), payloadBytes),
        _ => (nameof(LongOrchestration), 0),
    };

    if (command == "payload")
    {
        // ASCII characters occupy one UTF-8 byte each. JSON adds two quotes.
        Console.WriteLine($"ActivityOutputBytes={payloadBytes} JsonBytes={(long)payloadBytes + 2}");
    }

    string instanceId = Guid.NewGuid().ToString("N");
    Console.WriteLine($"InstanceId={instanceId}");
    await client.ScheduleNewOrchestrationInstanceAsync(
        name, input, new StartOrchestrationOptions(instanceId), cancellation);

    if (command == "start-long")
    {
        OrchestrationMetadata started = await client.WaitForInstanceStartAsync(instanceId, cancellation: cancellation);
        Console.WriteLine($"InstanceId={instanceId} status={started.RuntimeStatus}");
        return started.RuntimeStatus == OrchestrationRuntimeStatus.Running ? 0 : 1;
    }

    OrchestrationMetadata result = await client.WaitForInstanceCompletionAsync(
        instanceId, getInputsAndOutputs: true, cancellation);
    Console.WriteLine($"InstanceId={instanceId} status={result.RuntimeStatus} output={result.SerializedOutput}");
    if (result.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
    {
        Console.Error.WriteLine(result.FailureDetails?.ErrorMessage);
        return 1;
    }

    if (command == "run")
    {
        ChainResult output = result.ReadOutputAs<ChainResult>();
        return output == new ChainResult(4, 1) ? 0 : 1;
    }

    return result.ReadOutputAs<int>() == payloadBytes ? 0 : 1;
}
catch (OperationCanceledException) when (deadline.IsCancellationRequested)
{
    Console.Error.WriteLine("DTS operation timed out after 90 seconds. Check the worker log and instance status.");
    return 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}
finally
{
    using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await host.StopAsync(shutdown.Token);
}

sealed class ChainingOrchestration : TaskOrchestrator<int, ChainResult>
{
    public override async Task<ChainResult> RunAsync(TaskOrchestrationContext context, int count)
    {
        count = await context.CallActivityAsync<int>(nameof(IncrementActivity), count);
        count = await context.CallActivityAsync<int>(nameof(IncrementActivity), count);
        if (count == 2)
        {
            context.ContinueAsNew(count);
            return default;
        }

        return new ChainResult(count, 1);
    }
}

readonly record struct ChainResult(int ActivityCalls, int ContinueAsNewCount);

sealed class IncrementActivity : TaskActivity<int, int>
{
    public override Task<int> RunAsync(TaskActivityContext context, int input) => Task.FromResult(input + 1);
}

sealed class PayloadOrchestration : TaskOrchestrator<int, int>
{
    public override async Task<int> RunAsync(TaskOrchestrationContext context, int size)
    {
        string output = await context.CallActivityAsync<string>(nameof(PayloadActivity), size);
        return output.Length;
    }
}

sealed class PayloadActivity : TaskActivity<int, string>
{
    public override Task<string> RunAsync(TaskActivityContext context, int size) =>
        Task.FromResult(new string('x', size));
}

sealed class LongOrchestration : TaskOrchestrator<int, string>
{
    public override async Task<string> RunAsync(TaskOrchestrationContext context, int input)
    {
        await context.CreateTimer(TimeSpan.FromHours(1), CancellationToken.None);
        return "Timer expired";
    }
}
