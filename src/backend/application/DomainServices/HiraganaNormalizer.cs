using ComiCal.Domain.DomainServices;
using ComiCal.Domain.ValueObjects;

namespace ComiCal.Application.DomainServices;

/// <summary>
/// SQL 側の <c>dbo.fnToHiragana</c> と等価なロジックを純 C# で実装するノーマライザ。
/// 検索クエリ等を DB 計算列と突合する前に呼び出す。
/// </summary>
public sealed class HiraganaNormalizer : IHiraganaNormalizer
{
    /// <inheritdoc />
    public string ToHiragana(string raw) => HiraganaKey.ToHiragana(raw);
}
