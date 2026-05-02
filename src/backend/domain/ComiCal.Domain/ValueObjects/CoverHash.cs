using System.Security.Cryptography;

namespace ComiCal.Domain.ValueObjects;

public sealed class CoverHash : IEquatable<CoverHash>
{
    private const int HashLength = 32;

    public byte[] Value { get; }

    private CoverHash(byte[] value) => Value = value;

    public static CoverHash From(byte[] bytes)
    {
        if (bytes is null || bytes.Length != HashLength)
            throw new ArgumentException($"CoverHash must be exactly {HashLength} bytes.", nameof(bytes));
        var copy = new byte[HashLength];
        bytes.CopyTo(copy, 0);
        return new CoverHash(copy);
    }

    public static CoverHash ComputeFrom(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var hash = SHA256.HashData(stream);
        return new CoverHash(hash);
    }

    public bool Equals(CoverHash? other) =>
        other is not null && Value.AsSpan().SequenceEqual(other.Value.AsSpan());

    public override bool Equals(object? obj) => obj is CoverHash other && Equals(other);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        foreach (var b in Value) hc.Add(b);
        return hc.ToHashCode();
    }
}
