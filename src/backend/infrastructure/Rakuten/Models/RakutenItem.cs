using System.Text.Json.Serialization;

namespace ComiCal.Infrastructure.Rakuten.Models;

/// <summary>楽天 Books API の単一商品。</summary>
public sealed record RakutenItem
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("titleKana")]
    public string TitleKana { get; init; } = string.Empty;

    [JsonPropertyName("subTitle")]
    public string? SubTitle { get; init; }

    [JsonPropertyName("seriesName")]
    public string SeriesName { get; init; } = string.Empty;

    [JsonPropertyName("seriesNameKana")]
    public string SeriesNameKana { get; init; } = string.Empty;

    [JsonPropertyName("publisherName")]
    public string PublisherName { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("isbn")]
    public string Isbn { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("authorKana")]
    public string AuthorKana { get; init; } = string.Empty;

    [JsonPropertyName("salesDate")]
    public string SalesDate { get; init; } = string.Empty;

    [JsonPropertyName("itemPrice")]
    public int ItemPrice { get; init; }

    [JsonPropertyName("itemCaption")]
    public string? ItemCaption { get; init; }

    [JsonPropertyName("itemUrl")]
    public string? ItemUrl { get; init; }

    [JsonPropertyName("smallImageUrl")]
    public string? SmallImageUrl { get; init; }

    [JsonPropertyName("mediumImageUrl")]
    public string? MediumImageUrl { get; init; }

    [JsonPropertyName("largeImageUrl")]
    public string? LargeImageUrl { get; init; }

    [JsonPropertyName("booksGenreId")]
    public string? BooksGenreId { get; init; }
}
