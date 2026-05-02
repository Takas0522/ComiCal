namespace ComiCal.Domain.Entities;

public sealed class Author
{
    public Guid AuthorId { get; private set; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Author(Guid authorId, string name, string normalizedName, DateTime createdAt)
    {
        AuthorId = authorId;
        Name = name;
        NormalizedName = normalizedName;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Author Create(string name, string normalizedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        var now = DateTime.UtcNow;
        return new Author(
            Guid.NewGuid(),
            name[..Math.Min(name.Length, 128)],
            normalizedName[..Math.Min(normalizedName.Length, 128)],
            now);
    }

    public static Author CreateWithId(Guid authorId, string name, string normalizedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        var now = DateTime.UtcNow;
        return new Author(
            authorId,
            name[..Math.Min(name.Length, 128)],
            normalizedName[..Math.Min(normalizedName.Length, 128)],
            now);
    }
}
