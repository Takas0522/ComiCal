namespace ComiCal.Domain.Entities;

public sealed class Publisher
{
    public Guid PublisherId { get; private set; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Publisher(Guid publisherId, string name, string normalizedName, DateTime createdAt)
    {
        PublisherId = publisherId;
        Name = name;
        NormalizedName = normalizedName;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Publisher Create(string name, string normalizedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        var now = DateTime.UtcNow;
        return new Publisher(
            Guid.NewGuid(),
            name[..Math.Min(name.Length, 128)],
            normalizedName[..Math.Min(normalizedName.Length, 128)],
            now);
    }

    public void UpdateNormalizedName(string normalizedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        NormalizedName = normalizedName[..Math.Min(normalizedName.Length, 128)];
        UpdatedAt = DateTime.UtcNow;
    }
}
