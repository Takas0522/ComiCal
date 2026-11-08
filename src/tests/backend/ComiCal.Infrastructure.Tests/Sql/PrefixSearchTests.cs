using ComiCal.Domain.Queries;
using ComiCal.Infrastructure.Sql.Repositories;
using Xunit;

namespace ComiCal.Infrastructure.Tests.Sql;

public sealed class PrefixSearchTests(PrefixSearchFixture fixture) : IClassFixture<PrefixSearchFixture>
{
    [Theory]
    [InlineData("フランケン")]
    [InlineData("ふらんけん")]
    [InlineData("フランケン・ふらん Frantic")]
    [InlineData("Frantic")]
    public async Task SearchAsync_TitlePrefix_ReturnsMatchingActiveSeries(string keyword)
    {
        await using var db = fixture.CreateContext();
        var repository = new SeriesRepository(db);

        var (items, _) = await repository.SearchAsync(new SeriesSearchQuery(keyword, null, null, null));

        Assert.Equal(fixture.SeriesId, Assert.Single(items).SeriesId);
    }

    [Fact]
    public async Task SearchAsync_ExistingJapaneseWordPrefix_PreservesMatches()
    {
        await using var db = fixture.CreateContext();
        var repository = new SeriesRepository(db);

        var (items, _) = await repository.SearchAsync(new SeriesSearchQuery("ふらん", null, null, null));

        Assert.Contains(items, series => series.SeriesId == fixture.SeriesId);
        Assert.Equal(2, items.Count);
        Assert.All(items, series => Assert.False(series.IsDeleted));
    }

    [Theory]
    [InlineData("フランケン")]
    [InlineData("フランケン・ふらん Frantic")]
    [InlineData("木々津克久")]
    public async Task GetUpcomingAsync_TitleOrAuthorPrefix_ReturnsMatchingActiveVolumes(string keyword)
    {
        await using var db = fixture.CreateContext();
        var repository = new VolumeRepository(db);

        var (items, _) = await repository.GetUpcomingAsync(new UpcomingQuery(null, Keywords: [keyword]));

        Assert.Contains(items, volume => volume.VolumeId == fixture.VolumeId);
        Assert.All(items, volume => Assert.False(volume.IsDeleted));
        Assert.All(items, volume => Assert.False(volume.Series!.IsDeleted));
        if (keyword != "木々津克久")
            Assert.Single(items);
    }

    [Theory]
    [InlineData("フランケン")]
    [InlineData("フランケン・ふらん Frantic")]
    public async Task GetCalendarAsync_TitlePrefix_ReturnsMatchingActiveVolume(string keyword)
    {
        await using var db = fixture.CreateContext();
        var repository = new VolumeRepository(db);

        var items = await repository.GetCalendarAsync(new CalendarQuery(
            fixture.ReleaseDate.Year,
            fixture.ReleaseDate.Month,
            Keywords: ["該当なし", keyword]));

        Assert.Equal(fixture.VolumeId, Assert.Single(items).VolumeId);
    }
}
