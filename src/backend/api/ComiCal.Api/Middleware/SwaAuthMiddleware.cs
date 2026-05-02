using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Text;
using System.Text.Json;

namespace ComiCal.Api.Middleware;

public sealed class SwaAuthMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();
        if (req is not null)
        {
            var principal = req.Headers.TryGetValues("x-ms-client-principal", out var vals)
                ? vals.FirstOrDefault()
                : null;

            if (principal is not null)
            {
                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(principal));
                    var cp = JsonSerializer.Deserialize<SwaClientPrincipal>(json, JsonOptions);
                    if (cp is not null)
                        context.Items["SwaClientPrincipal"] = cp;
                }
                catch { /* ignore malformed header */ }
            }
        }
        await next(context);
    }
}

public record SwaClientPrincipal(
    string? IdentityProvider,
    string? UserId,
    string? UserDetails,
    IReadOnlyList<string>? UserRoles);
