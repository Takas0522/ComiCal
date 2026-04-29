using System.Text.Json.Serialization;

namespace ComiCal.Infrastructure.Rakuten.Models;

/// <summary>楽天 Books API（Books Total Search 20170404）のトップレベル応答。</summary>
public sealed record RakutenSearchResponse
{
    [JsonPropertyName("Items")]
    public IReadOnlyList<RakutenItemEnvelope> Items { get; init; } = Array.Empty<RakutenItemEnvelope>();

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageCount")]
    public int PageCount { get; init; }

    [JsonPropertyName("hits")]
    public int Hits { get; init; }

    [JsonPropertyName("first")]
    public int First { get; init; }

    [JsonPropertyName("last")]
    public int Last { get; init; }
}

/// <summary>各 <c>Items[*].Item</c> のラッパー。</summary>
public sealed record RakutenItemEnvelope
{
    [JsonPropertyName("Item")]
    public RakutenItem Item { get; init; } = new();
}
