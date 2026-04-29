using ComiCal.Domain.DomainServices;
using ComiCal.Domain.ValueObjects;

namespace ComiCal.Application.DomainServices;

/// <summary>ISBN-13 文字列を正規化して値オブジェクトに変換するノーマライザ。</summary>
public sealed class IsbnNormalizer : IIsbnNormalizer
{
    /// <inheritdoc />
    public Isbn13 Normalize(string raw) => Isbn13.Create(raw);
}
