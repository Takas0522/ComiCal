using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

public sealed class {{Name}}Activity
{
    private readonly ILogger<{{Name}}Activity> _logger;

    public {{Name}}Activity(ILogger<{{Name}}Activity> logger)
    {
        _logger = logger;
    }

    [Function(nameof({{Name}}Activity))]
    public async Task<{{ResponseType}}> Run(
        [ActivityTrigger] {{RequestType}} request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{{Name}}Activity started for {Key}", request.Key);

        // TODO: べき等な実装

        return new {{ResponseType}}();
    }
}
