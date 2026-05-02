using ComiCal.Domain.ValueObjects;
using Xunit;

namespace ComiCal.Domain.Tests.ValueObjects;

public sealed class ReleaseDateTests
{
    [Fact]
    public void FromDate_SpecificDate_DateMatchesAndIsMonthOnlyFalse()
    {
        // Arrange
        var date = new DateOnly(2025, 6, 15);

        // Act
        var rd = ReleaseDate.FromDate(date);

        // Assert
        Assert.Equal(date, rd.Date);
        Assert.False(rd.IsMonthOnly);
    }

    [Fact]
    public void FromYearMonth_IsMonthOnlyTrue_DateIsLastDayOfMonth()
    {
        // Act
        var rd = ReleaseDate.FromYearMonth(2025, 2);

        // Assert
        Assert.True(rd.IsMonthOnly);
        Assert.NotNull(rd.Date);
        Assert.Equal(new DateOnly(2025, 2, 28), rd.Date!.Value);
    }

    [Fact]
    public void FromYearMonth_LeapYear_DateIsLastDayOfFebruary()
    {
        // Act
        var rd = ReleaseDate.FromYearMonth(2024, 2);

        // Assert
        Assert.Equal(new DateOnly(2024, 2, 29), rd.Date!.Value);
    }

    [Fact]
    public void Tbd_DateIsNull_IsMonthOnlyFalse()
    {
        // Act
        var rd = ReleaseDate.Tbd();

        // Assert
        Assert.Null(rd.Date);
        Assert.False(rd.IsMonthOnly);
    }

    [Fact]
    public void Display_Tbd_ReturnsUndetermined()
    {
        // Arrange
        var rd = ReleaseDate.Tbd();

        // Act
        var display = rd.Display();

        // Assert
        Assert.Equal("未定", display);
    }

    [Fact]
    public void Display_FromDate_IncludesDay()
    {
        // Arrange
        var rd = ReleaseDate.FromDate(new DateOnly(2025, 6, 15));

        // Act
        var display = rd.Display();

        // Assert
        Assert.Contains("2025年", display);
        Assert.Contains("6月", display);
        Assert.Contains("15日", display);
    }

    [Fact]
    public void Display_FromYearMonth_ExcludesDay()
    {
        // Arrange
        var rd = ReleaseDate.FromYearMonth(2025, 6);

        // Act
        var display = rd.Display();

        // Assert
        Assert.Contains("2025年", display);
        Assert.Contains("6月", display);
        Assert.DoesNotContain("日", display);
    }
}
