namespace ComiCal.Domain.Entities;

/// <summary>
/// シリーズ集約ルート。<c>(NormalizedTitle, PrimaryAuthorId)</c> を集約キーとする。
/// </summary>
public sealed class Series
{
    private readonly List<SeriesAuthor> _authors = new();
    private readonly List<Volume> _volumes = new();

    /// <summary>シリーズ ID（PK）。</summary>
    public Guid Id { get; private set; }

    /// <summary>表示用タイトル。</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>正規化タイトル（半角化 / 記号除去 / 大小区別なし）。</summary>
    public string NormalizedTitle { get; private set; } = string.Empty;

    /// <summary>ひらがな正規化済みタイトル（フルテキスト検索用 PERSISTED 計算列の写像）。</summary>
    public string NormalizedTitleHiragana { get; private set; } = string.Empty;

    /// <summary>出版社 ID（NULL 許容）。</summary>
    public Guid? PublisherId { get; private set; }

    /// <summary>主著者 ID。</summary>
    public Guid PrimaryAuthorId { get; private set; }

    /// <summary>完結フラグ（Admin が手動で設定）。</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>シリーズに紐づく著者リレーション。</summary>
    public IReadOnlyList<SeriesAuthor> Authors => _authors;

    /// <summary>シリーズに属する巻（<see cref="Repositories.ISeriesRepository.GetWithVolumesAsync"/> 経由で読み込む場合のみ）。</summary>
    public IReadOnlyList<Volume> Volumes => _volumes;

    private Series()
    {
    }

    /// <summary>
    /// リポジトリ層から既存レコードを再構成するための復元ファクトリ。
    /// </summary>
    public static Series Hydrate(
        Guid id,
        string title,
        string normalizedTitle,
        string normalizedTitleHiragana,
        Guid? publisherId,
        Guid primaryAuthorId,
        bool isCompleted,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<SeriesAuthor>? authors = null,
        IEnumerable<Volume>? volumes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(normalizedTitle);
        ArgumentNullException.ThrowIfNull(normalizedTitleHiragana);

        var s = new Series
        {
            Id = id,
            Title = title,
            NormalizedTitle = normalizedTitle,
            NormalizedTitleHiragana = normalizedTitleHiragana,
            PublisherId = publisherId,
            PrimaryAuthorId = primaryAuthorId,
            IsCompleted = isCompleted,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
        if (authors is not null)
        {
            s._authors.AddRange(authors);
        }
        if (volumes is not null)
        {
            s._volumes.AddRange(volumes);
        }
        return s;
    }
}
