using ComiCal.Domain.ValueObjects;

namespace ComiCal.Domain.Entities;

/// <summary>
/// シリーズと著者を結ぶ関連エンティティ（<c>dbo.SeriesAuthors</c> に対応）。
/// </summary>
public sealed class SeriesAuthor
{
    /// <summary>関連 ID（PK）。</summary>
    public Guid Id { get; private set; }

    /// <summary>シリーズ ID。</summary>
    public Guid SeriesId { get; private set; }

    /// <summary>著者 ID。</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>役割。</summary>
    public AuthorRole Role { get; private set; }

    private SeriesAuthor()
    {
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static SeriesAuthor Hydrate(Guid id, Guid seriesId, Guid authorId, AuthorRole role)
        => new() { Id = id, SeriesId = seriesId, AuthorId = authorId, Role = role };
}
