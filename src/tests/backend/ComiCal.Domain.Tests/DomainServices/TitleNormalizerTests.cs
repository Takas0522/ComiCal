using ComiCal.Domain.DomainServices;
using Xunit;

namespace ComiCal.Domain.Tests.DomainServices;

public sealed class TitleNormalizerTests
{
    [Fact]
    public void Normalize_KatakanaToHiragana_Converts()
    {
        // Arrange & Act — ー (U+30FC) is outside ァ-ン range so stays as-is then stripped as symbol
        var result = TitleNormalizer.Normalize("ドラゴンボール");

        // Assert — katakana ド→ど, ラ→ら, ゴ→ご, ン→ん, ボ→ぼ, ー stays (non-letter stripped), ル→る
        Assert.Equal("どらごんぼーる", result);
    }

    [Fact]
    public void Normalize_FullWidthAlphanumeric_ConvertsToHalfWidth()
    {
        // Arrange & Act (Ａ = U+FF21, a = U+0061, ０ = U+FF10)
        var result = TitleNormalizer.Normalize("ＡＢＣＤ１２３");

        // Assert
        Assert.Equal("abcd123", result);
    }

    [Fact]
    public void Normalize_MultipleWhitespace_CollapsedToSingleSpace()
    {
        // Arrange & Act
        var result = TitleNormalizer.Normalize("進撃の  巨人");

        // Assert
        Assert.Equal("進撃の 巨人", result);
    }

    [Fact]
    public void Normalize_LeadingTrailingWhitespace_Trimmed()
    {
        // Arrange & Act
        var result = TitleNormalizer.Normalize("  タイトル  ");

        // Assert
        Assert.Equal("たいとる", result);
    }

    [Fact]
    public void Normalize_Symbols_Replaced()
    {
        // Arrange & Act
        var result = TitleNormalizer.Normalize("タイトル！？");

        // Assert — symbols removed and collapsed
        Assert.DoesNotContain("！", result);
        Assert.DoesNotContain("？", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhiteSpace_ThrowsArgumentException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TitleNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_OutputIsLowerCase()
    {
        // Arrange & Act
        var result = TitleNormalizer.Normalize("ABCdef");

        // Assert
        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void Normalize_MixedKatakanaAndHiragana_OnlyKatakanaConverted()
    {
        // Arrange & Act — ア(katakana) + あ(hiragana)
        var result = TitleNormalizer.Normalize("アあ");

        // Assert — ア→あ, already hiragana stays
        Assert.Equal("ああ", result);
    }
}
