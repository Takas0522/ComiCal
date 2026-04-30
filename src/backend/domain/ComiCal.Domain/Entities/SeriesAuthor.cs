using ComiCal.Domain.Enums;

namespace ComiCal.Domain.Entities;

public sealed class SeriesAuthor
{
    public Guid SeriesId { get; private set; }
    public Guid AuthorId { get; private set; }
    public SeriesAuthorRole Role { get; private set; }

    public Author? Author { get; private set; }

    private SeriesAuthor(Guid seriesId, Guid authorId, SeriesAuthorRole role)
    {
        SeriesId = seriesId;
        AuthorId = authorId;
        Role = role;
    }

    public static SeriesAuthor Create(Guid seriesId, Guid authorId, SeriesAuthorRole role)
        => new(seriesId, authorId, role);
}
