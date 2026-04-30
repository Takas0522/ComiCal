namespace ComiCal.Domain.Entities;

public sealed class Series
{
    public Guid SeriesId { get; private set; }
    public string Title { get; private set; }
    public string NormalizedTitle { get; private set; }
    public Guid PublisherId { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Publisher? Publisher { get; private set; }

    private readonly List<SeriesAuthor> _seriesAuthors = [];
    public IReadOnlyCollection<SeriesAuthor> SeriesAuthors => _seriesAuthors.AsReadOnly();

    private readonly List<Volume> _volumes = [];
    public IReadOnlyCollection<Volume> Volumes => _volumes.AsReadOnly();

    private Series(Guid seriesId, string title, string normalizedTitle, Guid publisherId, DateTime createdAt)
    {
        SeriesId = seriesId;
        Title = title;
        NormalizedTitle = normalizedTitle;
        PublisherId = publisherId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Series Create(string title, string normalizedTitle, Guid publisherId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedTitle);
        var now = DateTime.UtcNow;
        return new Series(
            Guid.NewGuid(),
            title[..Math.Min(title.Length, 256)],
            normalizedTitle[..Math.Min(normalizedTitle.Length, 256)],
            publisherId,
            now);
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSeriesAuthor(SeriesAuthor seriesAuthor)
    {
        ArgumentNullException.ThrowIfNull(seriesAuthor);
        _seriesAuthors.Add(seriesAuthor);
    }

    public void AddVolume(Volume volume)
    {
        ArgumentNullException.ThrowIfNull(volume);
        _volumes.Add(volume);
    }
}
