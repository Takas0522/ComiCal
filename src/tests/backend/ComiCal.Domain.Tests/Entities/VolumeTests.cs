using ComiCal.Domain.Entities;
using Xunit;

namespace ComiCal.Domain.Tests.Entities;

public sealed class VolumeTests
{
    [Fact]
    public void Create_ValidArguments_ReturnsVolumeWithNonEmptyId()
    {
        // Arrange
        var seriesId = Guid.NewGuid();

        // Act
        var volume = Volume.Create(seriesId, "9784088726236");

        // Assert
        Assert.NotEqual(Guid.Empty, volume.VolumeId);
        Assert.Equal(seriesId, volume.SeriesId);
        Assert.Equal("9784088726236", volume.Isbn13);
    }

    [Fact]
    public void Create_WithVolumeNumber_VolumeNumberSet()
    {
        // Act
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236", volumeNumber: 10);

        // Assert
        Assert.Equal(10, volume.VolumeNumber);
    }

    [Fact]
    public void Create_WithReleaseDate_ReleaseDateSet()
    {
        // Arrange
        var releaseDate = new DateTime(2025, 6, 15);

        // Act
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236", releaseDate: releaseDate);

        // Assert
        Assert.Equal(releaseDate, volume.ReleaseDate);
        Assert.False(volume.ReleaseDateIsMonthOnly);
    }

    [Fact]
    public void Create_MonthOnlyReleaseDate_ReleaseDateIsMonthOnlyTrue()
    {
        // Act
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236",
            releaseDate: new DateTime(2025, 6, 30), releaseDateIsMonthOnly: true);

        // Assert
        Assert.True(volume.ReleaseDateIsMonthOnly);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankIsbn_ThrowsArgumentException(string isbn)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Volume.Create(Guid.NewGuid(), isbn));
    }

    [Fact]
    public void UpdateCoverHash_SetsCoverHash()
    {
        // Arrange
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236");
        var hash = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        volume.UpdateCoverHash(hash);

        // Assert
        Assert.Equal(hash, volume.CoverHash);
    }

    [Fact]
    public void UpdateCoverHash_Null_ClearsCoverHash()
    {
        // Arrange
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236");
        volume.UpdateCoverHash(new byte[] { 0x01 });

        // Act
        volume.UpdateCoverHash(null);

        // Assert
        Assert.Null(volume.CoverHash);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrueAndDeletedAtNotNull()
    {
        // Arrange
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236");

        // Act
        volume.SoftDelete();

        // Assert
        Assert.True(volume.IsDeleted);
        Assert.NotNull(volume.DeletedAt);
    }

    [Fact]
    public void Create_IsDeletedDefaultFalse()
    {
        // Act
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236");

        // Assert
        Assert.False(volume.IsDeleted);
        Assert.Null(volume.CoverHash);
    }
}
