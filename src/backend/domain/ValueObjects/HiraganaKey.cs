namespace ComiCal.Domain.ValueObjects;

/// <summary>
/// 検索キー用にひらがな化された文字列を表す値オブジェクト。
/// <c>dbo.fnToHiragana</c>（全角カタカナ U+30A1..U+30F6 → ひらがな U+3041..U+3096）と等価な変換を行う。
/// </summary>
public sealed record HiraganaKey
{
    /// <summary>ひらがな正規化済み文字列。</summary>
    public string Value { get; }

    private HiraganaKey(string value) => Value = value;

    /// <summary>
    /// 入力文字列を <c>dbo.fnToHiragana</c> と等価に変換し、<see cref="HiraganaKey"/> を生成する。
    /// </summary>
    /// <param name="raw">任意の入力（<c>null</c> 不可）。</param>
    public static HiraganaKey Create(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return new HiraganaKey(ToHiragana(raw));
    }

    /// <summary>
    /// カタカナ → ひらがな変換のみを実施する純粋関数。SQL 側の <c>dbo.fnToHiragana</c> と挙動を一致させる。
    /// </summary>
    public static string ToHiragana(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.Length == 0)
        {
            return string.Empty;
        }
        var buffer = new char[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            buffer[i] = (c is >= '\u30A1' and <= '\u30F6')
                ? (char)(c - 0x60)
                : c;
        }
        return new string(buffer);
    }

    public override string ToString() => Value;
}
