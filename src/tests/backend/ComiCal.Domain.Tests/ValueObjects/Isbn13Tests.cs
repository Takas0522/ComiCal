using ComiCal.Domain.ValueObjects;
using Xunit;

namespace ComiCal.Domain.Tests.ValueObjects;

public sealed class Isbn13Tests
{
    [Theory]
    [InlineData("9784088726236")]
    [InlineData("9784088726243")]
    [InlineData("978-4-08-872623-6")]
    public void Constructor_ValidIsbn_CreatesInstance(string value)
    {
        // Act
        var isbn = new Isbn13(value);

        // Assert
        Assert.NotNull(isbn);
    }

    [Theory]
    [InlineData("9784088726236")]
    [InlineData("978-4-08-872623-6")]
    public void ToString_ReturnsDigitsOnly(string value)
    {
        // Arrange
        var isbn = new Isbn13(value);

        // Act
        var result = isbn.ToString();

        // Assert
        Assert.Equal("9784088726236", result);
        Assert.True(result.All(char.IsAsciiDigit));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("978408872623")]    // 12 digits
    [InlineData("97840887262360")]  // 14 digits
    [InlineData("978408872623X")]   // non-digit
    [InlineData("9784088726237")]   // wrong check digit
    public void Constructor_InvalidIsbn_ThrowsArgumentException(string value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Isbn13(value));
    }

    [Fact]
    public void Equals_SameDigits_ReturnsTrue()
    {
        // Arrange
        var a = new Isbn13("9784088726236");
        var b = new Isbn13("978-4-08-872623-6");

        // Assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var a = new Isbn13("9784088726236");
        var b = new Isbn13("9784088726243");

        // Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsDigits()
    {
        // Arrange
        var isbn = new Isbn13("9784088726236");

        // Act
        string s = isbn;

        // Assert
        Assert.Equal("9784088726236", s);
    }

    [Fact]
    public void ExplicitConversion_FromString_CreatesInstance()
    {
        // Act
        var isbn = (Isbn13)"9784088726236";

        // Assert
        Assert.Equal("9784088726236", isbn.ToString());
    }
}
