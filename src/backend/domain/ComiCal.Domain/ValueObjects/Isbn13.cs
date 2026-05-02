namespace ComiCal.Domain.ValueObjects;

public sealed class Isbn13 : IEquatable<Isbn13>
{
    private readonly string _value;

    public Isbn13(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var digits = value.Replace("-", "", StringComparison.Ordinal).Trim();
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException("ISBN-13 must be exactly 13 digits.", nameof(value));
        if (!IsValidCheckDigit(digits))
            throw new ArgumentException("ISBN-13 check digit is invalid.", nameof(value));
        _value = digits;
    }

    private static bool IsValidCheckDigit(string digits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        var check = (10 - sum % 10) % 10;
        return check == (digits[12] - '0');
    }

    public override string ToString() => _value;

    public bool Equals(Isbn13? other) => other is not null && _value == other._value;
    public override bool Equals(object? obj) => obj is Isbn13 other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode(StringComparison.Ordinal);

    public static explicit operator Isbn13(string value) => new(value);
    public static implicit operator string(Isbn13 isbn) => isbn._value;
}
