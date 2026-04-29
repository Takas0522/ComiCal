namespace ComiCal.Domain.DomainServices;

/// <summary>
/// 任意の文字列を <c>dbo.fnToHiragana</c> と等価なロジックでひらがな化するドメインサービス。
/// 検索クエリのフロント側（C# 側）正規化に用いる。
/// </summary>
public interface IHiraganaNormalizer
{
    /// <summary>カタカナ → ひらがな変換を実施する。</summary>
    string ToHiragana(string raw);
}
