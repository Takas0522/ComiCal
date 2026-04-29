namespace ComiCal.Domain.ValueObjects;

/// <summary>
/// シリーズに紐づく著者の役割。<c>dbo.SeriesAuthors.Role</c> の CHECK 制約に対応する。
/// </summary>
public enum AuthorRole
{
    /// <summary>主著者（シリーズ集約キーの一部となる 1 名）。</summary>
    Primary = 0,

    /// <summary>共著者。</summary>
    Co = 1,

    /// <summary>原作者。</summary>
    Original = 2,
}
