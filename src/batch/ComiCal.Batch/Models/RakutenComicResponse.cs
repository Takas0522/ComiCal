using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text;

namespace ComiCal.Batch.Models
{

    public class RakutenComicResponse
    {
        [JsonPropertyName("Items")]
        public IEnumerable<ComicInfo> Comics { get; set; }
        [JsonPropertyName("pageCount")]
        public int PageCount { get; set; }
        [JsonPropertyName("hits")]
        public int Hits { get; set; }
        [JsonPropertyName("last")]
        public int Last { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("page")]
        public int Page { get; set; }
        [JsonPropertyName("carrier")]
        public int Carrier { get; set; }
        [JsonPropertyName("GenreInformation")]
        public IEnumerable<object> GenreInformation { get; set; }
        [JsonPropertyName("first")]
        public int First { get; set; }
    }

    public class ComicInfo
    {
        [JsonPropertyName("Item")]
        public ComicInfos Info { get; set; }
    }

    public class ComicInfos
    {
        [JsonPropertyName("limitedFlag")]
        public int LimitedFlag { get; set; }
        [JsonPropertyName("authorKana")]
        public string AuthorKana { get; set; }
        [JsonPropertyName("author")]
        public string Author { get; set; }
        [JsonPropertyName("subTitle")]
        public string subTitle { get; set; }
        [JsonPropertyName("seriesNameKana")]
        public string SeriesNameKana { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("subTitleKana")]
        public string SubTitleKana { get; set; }
        [JsonPropertyName("itemCaption")]
        public string ItemCaption { get; set; }
        [JsonPropertyName("publisherName")]
        public string PublisherName { get; set; }
        [JsonPropertyName("listPrice")]
        public int ListPrice { get; set; }
        [JsonPropertyName("isbn")]
        public string Isbn { get; set; }
        [JsonPropertyName("largeImageUrl")]
        public string LargeImageUrl { get; set; }
        [JsonPropertyName("mediumImageUrl")]
        public string MediumImageUrl { get; set; }
        [JsonPropertyName("titleKana")]
        public string TitleKana { get; set; }
        [JsonPropertyName("availability")]
        public string Availability { get; set; }
        [JsonPropertyName("postageFlag")]
        public int PostageFlag { get; set; }
        [JsonPropertyName("salesDate")]
        public string SalesDate { get; set; }
        [JsonPropertyName("contents")]
        public string Contents { get; set; }
        [JsonPropertyName("smallImageUrl")]
        public string SmallImageUrl { get; set; }
        [JsonPropertyName("discountPrice")]
        public int DiscountPrice { get; set; }
        [JsonPropertyName("itemPrice")]
        public int ItemPrice { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }
        [JsonPropertyName("booksGenreId")]
        public string BooksGenreId { get; set; }
        [JsonPropertyName("affiliateUrl")]
        public string AffiliateUrl { get; set; }
        [JsonPropertyName("seriesName")]
        public string SeriesName { get; set; }
        [JsonPropertyName("reviewCount")]
        public int ReviewCount { get; set; }
        [JsonPropertyName("reviewAverage")]
        public string ReviewAverage { get; set; }
        [JsonPropertyName("discountRate")]
        public int DiscountRate { get; set; }
        [JsonPropertyName("chirayomiUrl")]
        public string ChirayomiUrl { get; set; }
        [JsonPropertyName("itemUrl")]
        public string ItemUrl { get; set; }
    }

}
