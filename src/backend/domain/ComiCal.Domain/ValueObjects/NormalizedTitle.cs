namespace ComiCal.Domain.ValueObjects;

public sealed record NormalizedTitle
{
    public string Value { get; }

    private NormalizedTitle(string value) => Value = value;

    public static NormalizedTitle From(string rawTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawTitle);
        return new NormalizedTitle(rawTitle.Trim());
    }

    public override string ToString() => Value;
}
