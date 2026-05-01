using ComiCal.Batch.Activities;
using ComiCal.Batch.Models;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Activities;

public sealed class UpsertVolumesActivityTests
{
    private readonly IVolumeRepository _volumeRepo = Substitute.For<IVolumeRepository>();
    private readonly ISeriesRepository _seriesRepo = Substitute.For<ISeriesRepository>();
    private readonly IAuthorRepository _authorRepo = Substitute.For<IAuthorRepository>();
    private readonly IPublisherRepository _publisherRepo = Substitute.For<IPublisherRepository>();
    private readonly IBatchRunRepository _batchRunRepo = Substitute.For<IBatchRunRepository>();
    private readonly UpsertVolumesActivity _sut;

    public UpsertVolumesActivityTests()
    {
        _sut = new UpsertVolumesActivity(
            _volumeRepo,
            _seriesRepo,
            _authorRepo,
            _publisherRepo,
            _batchRunRepo,
            Substitute.For<ILogger<UpsertVolumesActivity>>());
    }

    // ── new ISBN ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_NewIsbn_WithImage_InsertsAndQueuesThumbnail()
    {
        const string isbn = "9784088726236";
        _volumeRepo.FindByIsbnAsync(isbn, Arg.Any<CancellationToken>()).Returns((Volume?)null);
        _volumeRepo.UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var input = new UpsertVolumesInput(Guid.NewGuid(), [
            new RakutenVolumeData(isbn, "テスト 1巻", "著者A", "出版社", "2025-06-15",
                "https://img.example.com/1.jpg", null),
        ]);

        var result = await _sut.Run(input);

        Assert.Equal(1, result.UpsertedCount);
        Assert.Single(result.ThumbnailPending);
        Assert.Empty(result.FailedIsbn13s);
        await _volumeRepo.Received(1).UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_NewIsbn_NoImage_NoPendingThumbnail()
    {
        const string isbn = "9784088726237";
        _volumeRepo.FindByIsbnAsync(isbn, Arg.Any<CancellationToken>()).Returns((Volume?)null);
        _volumeRepo.UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var input = new UpsertVolumesInput(Guid.NewGuid(), [
            new RakutenVolumeData(isbn, "テスト 2巻", "著者A", "出版社", "2025-07-15", null, null),
        ]);

        var result = await _sut.Run(input);

        Assert.Equal(1, result.UpsertedCount);
        Assert.Empty(result.ThumbnailPending);
    }

    // ── existing ISBN ────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ExistingIsbn_QueuesThumnailWithExistingHashForSkipDetection()
    {
        // The existing CoverHash is passed to ThumbnailPendingItem so DownloadThumbnailActivity
        // can compare hashes and skip the download when the image is unchanged.
        const string isbn = "9784088726236";
        var existingVolume = Volume.Create(Guid.NewGuid(), isbn);
        var existingHash = new byte[] { 0x11, 0x22, 0x33 };
        existingVolume.UpdateCoverHash(existingHash);
        _volumeRepo.FindByIsbnAsync(isbn, Arg.Any<CancellationToken>()).Returns(existingVolume);
        _volumeRepo.UpsertAsync(existingVolume, Arg.Any<CancellationToken>()).Returns(existingVolume.VolumeId);

        var input = new UpsertVolumesInput(Guid.NewGuid(), [
            new RakutenVolumeData(isbn, "テスト 1巻", "著者A", "出版社", "2025-06-15",
                "https://img.example.com/1.jpg", "https://item.example.com/"),
        ]);

        var result = await _sut.Run(input);

        Assert.Equal(1, result.UpsertedCount);
        Assert.Single(result.ThumbnailPending);
        Assert.Equal(existingHash, result.ThumbnailPending[0].ExistingHash);
        Assert.Equal(existingVolume.VolumeId, result.ThumbnailPending[0].VolumeId);
        await _volumeRepo.Received(1).UpsertAsync(existingVolume, Arg.Any<CancellationToken>());
    }

    // ── invalid ISBN ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("123")]             // too short
    [InlineData("12345678901234")]  // 14 digits — too long
    [InlineData("978408872623A")]   // contains non-digit
    public async Task RunAsync_InvalidIsbn_SkippedAndAddedToFailedList(string invalidIsbn)
    {
        var input = new UpsertVolumesInput(Guid.NewGuid(), [
            new RakutenVolumeData(invalidIsbn, "タイトル", "著者", "出版社", "", null, null),
        ]);

        var result = await _sut.Run(input);

        Assert.Equal(0, result.UpsertedCount);
        Assert.Single(result.FailedIsbn13s);
        Assert.Equal(invalidIsbn, result.FailedIsbn13s[0]);
        await _volumeRepo.DidNotReceive().UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>());
    }

    // ── exception handling ───────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_UpsertThrows_CatchesAndRecordsFailedItem()
    {
        const string isbn = "9784088726236";
        _volumeRepo.FindByIsbnAsync(isbn, Arg.Any<CancellationToken>()).Returns((Volume?)null);
        _volumeRepo.UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Guid>(new InvalidOperationException("DB error")));

        var input = new UpsertVolumesInput(Guid.NewGuid(), [
            new RakutenVolumeData(isbn, "テスト 1巻", "著者A", "出版社", "2025-06-15", null, null),
        ]);

        var result = await _sut.Run(input);

        Assert.Equal(0, result.UpsertedCount);
        Assert.Single(result.FailedIsbn13s);
        await _batchRunRepo.Received(1)
            .AddFailedItemAsync(Arg.Any<FailedItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_MultipleItems_FailuresDoNotPreventOtherItems()
    {
        const string goodIsbn = "9784088726236";
        const string badIsbn = "9784088726237";

        _volumeRepo.FindByIsbnAsync(goodIsbn, Arg.Any<CancellationToken>()).Returns((Volume?)null);
        _volumeRepo.FindByIsbnAsync(badIsbn, Arg.Any<CancellationToken>()).Returns((Volume?)null);
        _volumeRepo.UpsertAsync(
            Arg.Is<Volume>(v => v.Isbn13 == goodIsbn), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        _volumeRepo.UpsertAsync(
            Arg.Is<Volume>(v => v.Isbn13 == badIsbn), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Guid>(new InvalidOperationException("DB error")));

        var input = new UpsertVolumesInput(Guid.NewGuid(), [
            new RakutenVolumeData(goodIsbn, "成功 1巻", "著者", "出版社", "2025-06-15", null, null),
            new RakutenVolumeData(badIsbn, "失敗 2巻", "著者", "出版社", "2025-07-15", null, null),
        ]);

        var result = await _sut.Run(input);

        Assert.Equal(1, result.UpsertedCount);
        Assert.Single(result.FailedIsbn13s);
        Assert.Equal(badIsbn, result.FailedIsbn13s[0]);
    }
}
