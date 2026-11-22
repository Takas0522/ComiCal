using Azure.Core;
using Azure.Storage.Blobs;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Jobs;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Sql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace ComiCal.Batch.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class DailyBatchIntegrationTests(BatchStorageFixture storage, ITestOutputHelper output) : IClassFixture<BatchStorageFixture>
{
    private static readonly byte[] Cover = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jS9sAAAAASUVORK5CYII=");

    [Fact]
    public async Task Run_PersistsBothPagesAndCovers_ThenSkipsCompletedJstDay()
    {
        // BlobStorageService creates its own HttpClient, so serve the image on a local ephemeral port.
        var requestedCovers = new ConcurrentBag<string>();
        var web = WebApplication.CreateSlimBuilder();
        web.WebHost.UseUrls("http://127.0.0.1:0");
        await using var images = web.Build();
        images.MapGet("/covers/{isbn}", (string isbn) =>
        {
            requestedCovers.Add(isbn);
            return Results.Bytes(Cover, "image/png");
        });
        await images.StartAsync();

        var imageBase = images.Urls.Single() + "/covers/";
        var first = Book("9784253019644", "月の旅 1");
        var second = Book("9784253019645", "月の旅 2");
        var third = Book("9784253019646", "海の旅 1");
        var pages = new Dictionary<int, RakutenBookItem[]> { [1] = [first, second], [2] = [third] };
        var requestedPages = new List<int>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SqlConnectionString"] = storage.SqlConnectionString,
            ["StorageAccountUri"] = storage.BlobConnectionString,
            ["RakutenApplicationId"] = "test-app",
            ["RakutenAccessKey"] = "test-key",
            ["RakutenAffiliateId"] = "test-affiliate",
        }).Build();

        var services = new ServiceCollection();
        using var logger = new LoggerConfiguration().MinimumLevel.Warning().WriteTo.Sink(new TestLogSink(output)).CreateLogger();
        services.AddLogging(builder => builder.AddSerilog(logger));
        services.AddBatchInfrastructure(configuration);
        services.AddHttpClient<RakutenBooksApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new RakutenHandler(pages, requestedPages));

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;
        var sqlChecks = new PreflightChecks(Substitute.For<TokenCredential>(), provider.GetRequiredService<IHttpClientFactory>(),
            new KeyVaultSettings(configuration, Substitute.For<IBatchSecretSource>()), provider.GetRequiredService<IServiceScopeFactory>(), configuration);
        var checks = Substitute.For<IPreflightChecks>();
        checks.CheckSqlAsync(Arg.Any<CancellationToken>()).Returns(call => sqlChecks.CheckSqlAsync(call.Arg<CancellationToken>()));

        var work = Substitute.For<IBatchWork>();
        work.WarmupAsync(Arg.Any<CancellationToken>()).Returns(call =>
            scoped.GetRequiredService<SqlWarmup>().RunAsync(call.Arg<CancellationToken>()));
        work.FetchAsync(Arg.Any<CancellationToken>()).Returns(call =>
            provider.GetRequiredService<DailyFetchJob>().RunAsync(call.Arg<CancellationToken>()));

        // 実際の Mutex（Global\ComiCal.Batch）は並列実行される SingleInstanceGuardTests と競合するため、
        // ここでは action をそのまま実行するスタブを使う（排他は専用テストで検証済み）。
        var guard = Substitute.For<ISingleInstanceGuard>();
        guard.RunExclusive(Arg.Any<Func<int>>()).Returns(call => call.Arg<Func<int>>()());

        var command = BatchCommand.Parse(["run"]);
        Assert.Equal(ExitCodes.Success, Runner(TimeProvider.System).Execute(command, TextWriter.Null, CancellationToken.None));

        var db = scoped.GetRequiredService<ComiCalDbContext>();
        var run = Assert.Single(await db.BatchRuns.AsNoTracking().ToListAsync());
        Assert.Equal(BatchRunStatus.Succeeded, run.Status);
        Assert.Equal(3, run.FetchedItemCount);
        Assert.Equal(3, run.UpsertedVolumeCount);
        Assert.Equal(3, run.DownloadedThumbnailCount);
        Assert.Equal(0, run.FailedItemCount);

        var volumes = await db.Volumes.AsNoTracking().OrderBy(volume => volume.Isbn13).ToListAsync();
        string[] expectedIsbns = [first.Isbn, second.Isbn, third.Isbn];
        Assert.Equal(expectedIsbns, volumes.Select(volume => volume.Isbn13));
        // シリーズの名寄せ規則はドメイン側のテストで検証する。ここでは各巻がシリーズに紐づくことだけを確認する。
        var seriesIds = volumes.Select(volume => volume.SeriesId).ToHashSet();
        Assert.DoesNotContain(Guid.Empty, seriesIds);
        Assert.Equal(seriesIds.Count, await db.Series.CountAsync(series => seriesIds.Contains(series.SeriesId)));

        var assets = await db.ThumbnailAssets.AsNoTracking().ToListAsync();
        Assert.Equal(volumes.Select(volume => volume.VolumeId).Order(), assets.Select(asset => asset.VolumeId).Order());
        var covers = provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("covers");
        foreach (var asset in assets)
        {
            var blob = await covers.GetBlobClient(asset.BlobKey).DownloadContentAsync();
            Assert.Equal(Cover, blob.Value.Content.ToArray());
        }

        // HasSucceededAsync は CompletedAt を JST の当日範囲と比較するので、完了時刻で時計を固定する
        // （1 回目の実行が JST の午前 0 時をまたいでも 2 回目が同じ日として判定される）。
        var completedAt = Assert.NotNull(run.CompletedAt);
        var sameDay = new FixedTimeProvider(new DateTimeOffset(DateTime.SpecifyKind(completedAt, DateTimeKind.Utc)));
        Assert.Equal(ExitCodes.Success, Runner(sameDay).Execute(command, TextWriter.Null, CancellationToken.None));
        await work.Received(1).FetchAsync(Arg.Any<CancellationToken>());
        await work.Received(1).WarmupAsync(Arg.Any<CancellationToken>());
        await checks.Received(2).CheckSqlAsync(Arg.Any<CancellationToken>());
        Assert.Equal(run.BatchRunId, (await db.BatchRuns.AsNoTracking().SingleAsync()).BatchRunId);
        Assert.Collection(requestedPages, page => Assert.Equal(1, page), page => Assert.Equal(2, page));
        Assert.Equal(expectedIsbns, requestedCovers.Order(StringComparer.Ordinal));

        RakutenBookItem Book(string isbn, string title) => new(isbn, title, "著者", "出版社",
            DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), imageBase + isbn, "https://books.example/" + isbn);

        BatchRunner Runner(TimeProvider clock) => new(guard,
            new Lazy<IStaleBatchRunCleaner>(() => ActivatorUtilities.CreateInstance<StaleBatchRunCleaner>(scoped)),
            new Lazy<IBatchRunRepository>(scoped.GetRequiredService<IBatchRunRepository>),
            new Preflight(checks, provider.GetRequiredService<ILogger<Preflight>>()), work,
            Substitute.For<IFailureNotifier>(), Substitute.For<IHeartbeat>(), configuration, clock,
            provider.GetRequiredService<ILogger<BatchRunner>>());
    }

    private sealed class TestLogSink(ITestOutputHelper output) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) =>
            output.WriteLine("{0} {1}", logEvent.RenderMessage(CultureInfo.InvariantCulture), logEvent.Exception);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RakutenHandler(Dictionary<int, RakutenBookItem[]> pages, List<int> requestedPages) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var uri = request.RequestUri!;
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (uri.Host != "openapi.rakuten.co.jp" ||
                !int.TryParse(query["page"], CultureInfo.InvariantCulture, out var page) ||
                !pages.TryGetValue(page, out var items))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

            requestedPages.Add(page);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RakutenBooksSearchResult(3, page, pages.Count, items.Length, items))
            });
        }
    }
}
