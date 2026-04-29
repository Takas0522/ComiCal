using ComiCal.Domain.Entities;
using ComiCal.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Unit;

public sealed class VolumeTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsVolumeWithGuidV7Id()
    {
        var seriesId = System.Guid.CreateVersion7();
        var isbn = Isbn13.Create("9784088838212");
        var releaseDate = new System.DateOnly(2026, 4, 1);
        var coverHash = new System.ReadOnlyMemory<byte>(new byte[32]);

        var volume = Volume.Create(
            seriesId,
            isbn,
            volumeNumber: 108,
            releaseDate: releaseDate,
            releaseDateIsMonthOnly: false,
            coverHash: coverHash,
            rakutenItemUrl: "https://books.rakuten.co.jp/rb/12345/");

        volume.Should().NotBeNull();
        volume.Id.Should().NotBe(System.Guid.Empty);
        volume.SeriesId.Should().Be(seriesId);
        volume.Isbn.Should().Be(isbn);
        volume.VolumeNumber.Should().Be(108);
        volume.ReleaseDate.Should().Be(releaseDate);
        volume.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptySeriesId_Throws()
    {
        var isbn = Isbn13.Create("9784088838212");

        var act = () => Volume.Create(
            System.Guid.Empty,
            isbn,
            volumeNumber: null,
            releaseDate: null,
            releaseDateIsMonthOnly: false,
            coverHash: System.ReadOnlyMemory<byte>.Empty,
            rakutenItemUrl: null);

        act.Should().Throw<System.ArgumentException>();
    }
}
