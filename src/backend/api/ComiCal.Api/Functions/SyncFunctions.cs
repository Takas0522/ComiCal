using ComiCal.Api.Extensions;
using ComiCal.Infrastructure.Blob;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class SyncFunctions
{
    [Function("UploadSyncQr")]
    public static async Task<HttpResponseData> UploadAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/me/sync/qr")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        var body = await req.ReadJsonAsync<SyncQrBody>();
        if (body is null || string.IsNullOrWhiteSpace(body.EncryptedPayload))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            bad.Headers.Add("Content-Type", "application/problem+json");
            await bad.WriteAsJsonAsync(new
            {
                type = "https://comical.example.jp/errors/validation",
                title = "encryptedPayload is required",
                status = 400
            }, ct);
            return bad;
        }

        var blobService = ctx.InstanceServices.GetRequiredService<BlobStorageService>();
        var (token, expiresAt) = await blobService.UploadSyncQrDataAsync(body.EncryptedPayload, ct);

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new { token, expiresAt }, ct);
        return res;
    }

    [Function("GetSyncQrData")]
    public static async Task<HttpResponseData> GetAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/me/sync/qr/{token}")] HttpRequestData req,
        string token,
        FunctionContext ctx,
        CancellationToken ct)
    {
        var blobService = ctx.InstanceServices.GetRequiredService<BlobStorageService>();
        var data = await blobService.GetSyncQrDataAsync(token, ct);
        if (data is null)
            return req.CreateResponse(HttpStatusCode.NotFound);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { encryptedPayload = data }, ct);
        return res;
    }

    private sealed record SyncQrBody(string? EncryptedPayload);
}
