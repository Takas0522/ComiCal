using ComiCal.Batch.Activities;
using ComiCal.Batch.Models;
using ComiCal.Batch.Tests.TestHelpers;
using ComiCal.Infrastructure.Rakuten;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;
using System.Threading.RateLimiting;
using Xunit;

namespace ComiCal.Batch.Tests.Activities;

public sealed class FetchPageActivityTests
{
    private static FetchPageActivity CreateSut(string responseJson)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(responseJson));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Rakuten-AppId", "test-id");

        var rateLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
        });

        var rakutenClient = new RakutenBooksApiClient(
            httpClient, rateLimiter, Substitute.For<ILogger<RakutenBooksApiClient>>());
        return new FetchPageActivity(rakutenClient, Substitute.For<ILogger<FetchPageActivity>>());
    }

    /// <summary>Builds minimal Rakuten Books JSON with the given items.</summary>
    private static string BuildJson(int last, IEnumerable<object> items)
    {
        var list = items.ToList();
        return JsonSerializer.Serialize(new
        {
            count = list.Count,
            page = 1,
            last,
            hits = list.Count,
            Items = list,
        });
    }

    private static object RakutenItem(string isbn, string title = "テスト巻", string imageUrl = "", string itemUrl = "https://item.example.com/")
        => new { isbn, title, author = "著者A", publisherName = "出版社", salesDate = "2025-06-15", largeImageUrl = imageUrl, itemUrl };

    // ── happy-path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_MapsItemsAndReturnsCorrectCounts()
    {
        var sut = CreateSut(BuildJson(3, [
            RakutenItem("9784088726236", imageUrl: "https://img.example.com/1.jpg"),
            RakutenItem("9784088726237"),
        ]));
        var input = new FetchPageInput(Guid.NewGuid(), 1, DateOnly.FromDateTime(DateTime.Today), null);

        var result = await sut.RunAsync(Substitute.For<TaskActivityContext>(), input);

        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.FetchedCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("9784088726236", result.Items[0].Isbn13);
    }

    // ── LargeImageUrl normalisation ─────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_BlankLargeImageUrl_MappedToNull(string blankUrl)
    {
        var sut = CreateSut(BuildJson(1, [RakutenItem("9784088726236", imageUrl: blankUrl)]));

        var result = await sut.RunAsync(Substitute.For<TaskActivityContext>(),
            new FetchPageInput(Guid.NewGuid(), 1, null, null));

        Assert.Null(result.Items[0].LargeImageUrl);
    }

    [Fact]
    public async Task RunAsync_NonEmptyLargeImageUrl_PreservedInOutput()
    {
        const string imageUrl = "https://img.example.com/cover.jpg";
        var sut = CreateSut(BuildJson(1, [RakutenItem("9784088726236", imageUrl: imageUrl)]));

        var result = await sut.RunAsync(Substitute.For<TaskActivityContext>(),
            new FetchPageInput(Guid.NewGuid(), 1, null, null));

        Assert.Equal(imageUrl, result.Items[0].LargeImageUrl);
    }
}
