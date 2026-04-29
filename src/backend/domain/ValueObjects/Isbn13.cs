namespace ComiCal.Domain.ValueObjects;

/// <summary>
/// ISBN-13 を表す値オブジェクト。
/// 13 桁数字 + EAN-13 チェックディジット検証を行い、不変として保持する。
/// </summary>
public sealed record Isbn13
{
    /// <summary>正規化済みの ISBN-13 文字列（ハイフンなし、13 桁数字）。</summary>
    public string Value { get; }

    private Isbn13(string value) => Value = value;

    /// <summary>
    /// 入力文字列を正規化し、ISBN-13 として有効な場合に <see cref="Isbn13"/> を生成する。
    /// </summary>
    /// <param name="raw">ハイフンや空白を含み得る生の ISBN-13 文字列。</param>
    /// <returns>正規化された <see cref="Isbn13"/>。</returns>
    /// <exception cref="ArgumentException">空文字、桁数不一致、または非数字、もしくはチェックディジット不一致のとき。</exception>
    public static Isbn13 Create(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        if (!TryCreate(raw, out var value, out var error))
        {
            throw new ArgumentException(error, nameof(raw));
        }
        return value!;
    }

    /// <summary>例外を投げずに ISBN-13 を生成する。</summary>
    public static bool TryCreate(string? raw, out Isbn13? value, out string? error)
    {
        value = null;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "ISBN-13 must not be empty.";
            return false;
        }

        var normalized = Normalize(raw);
        if (normalized.Length != 13)
        {
            error = "ISBN-13 must be 13 digits.";
            return false;
        }

        for (var i = 0; i < 13; i++)
        {
            if (!char.IsDigit(normalized[i]))
            {
                error = "ISBN-13 must contain only digits.";
                return false;
            }
        }

        if (!HasValidChecksum(normalized))
        {
            error = "ISBN-13 checksum digit is invalid.";
            return false;
        }

        value = new Isbn13(normalized);
        return true;
    }

    /// <summary>ハイフン / 空白を除去しトリミングする。チェックディジット検証は行わない。</summary>
    public static string Normalize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return raw
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u3000", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static bool HasValidChecksum(string digits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var d = digits[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }
        var check = (10 - (sum % 10)) % 10;
        return check == digits[12] - '0';
    }

    public override string ToString() => Value;
}
