using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Rakuten.Models;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Unit;

public sealed class RakutenItemMapperTests
{
    [Fact]
    public void TryMap_with_valid_item_returns_payload()
    {
        var item = new RakutenItem
        {
            Title = "ワンピース 100",
            SeriesName = "ワンピース",
            SeriesNameKana = "ONE PIECE",
            PublisherName = "集英社",
            Author = "尾田 栄一郎",
            AuthorKana = "オダ エイイチロウ",
            Isbn = "9784088100005",
            SalesDate = "2026年04月03日",
            ItemUrl = "https://books.rakuten.co.jp/rb/1000/",
            LargeImageUrl = "https://example.com/cover.jpg",
        };

        var payload = RakutenItemMapper.TryMap(item);

        payload.Should().NotBeNull();
        payload!.Isbn.Value.Should().Be("9784088100005");
        payload.SeriesName.Should().Be("ワンピース");
        payload.VolumeNumber.Should().Be(100);
        payload.ReleaseDate.Should().Be(new DateOnly(2026, 4, 3));
        payload.ReleaseDateIsMonthOnly.Should().BeFalse();
        payload.AuthorName.Should().Be("尾田 栄一郎");
        payload.PublisherName.Should().Be("集英社");
        payload.CoverImageUrl.Should().Be("https://example.com/cover.jpg");
    }

    [Fact]
    public void TryMap_with_blank_isbn_returns_null()
    {
        var item = new RakutenItem { Title = "test 1", Isbn = string.Empty, SalesDate = "2026年04月15日" };
        RakutenItemMapper.TryMap(item).Should().BeNull();
    }

    [Fact]
    public void TryMap_with_short_isbn_returns_null()
    {
        var item = new RakutenItem { Title = "test 1", Isbn = "12345", SalesDate = "2026年04月15日" };
        RakutenItemMapper.TryMap(item).Should().BeNull();
    }

    [Fact]
    public void ParseSalesDate_handles_month_only()
    {
        var (date, isMonthOnly) = RakutenItemMapper.ParseSalesDate("2026年04月");
        isMonthOnly.Should().BeTrue();
        date.Should().Be(new DateOnly(2026, 4, 30));
    }

    [Fact]
    public void ParseSalesDate_handles_full_date()
    {
        var (date, isMonthOnly) = RakutenItemMapper.ParseSalesDate("2026年04月03日");
        isMonthOnly.Should().BeFalse();
        date.Should().Be(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void ParseSalesDate_handles_blank()
    {
        var (date, isMonthOnly) = RakutenItemMapper.ParseSalesDate(null);
        date.Should().BeNull();
        isMonthOnly.Should().BeFalse();
    }

    [Theory]
    [InlineData("ワンピース 100", 100)]
    [InlineData("葬送のフリーレン 12", 12)]
    [InlineData("タイトルなし", null)]
    public void ExtractVolumeNumber_extracts_trailing_digits(string title, int? expected)
    {
        RakutenItemMapper.ExtractVolumeNumber(title).Should().Be(expected);
    }
}
