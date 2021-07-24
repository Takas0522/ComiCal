using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ComiCal.Batch.Models
{

    public class RakutenComicResponse
    {
        [DataMember(Name = "Items")]
        public IEnumerable<ComicInfo> Comics { get; set; }
        [DataMember(Name = "pageCount")]
        public int PageCount { get; set; }
        [DataMember(Name = "hits")]
        public int Hits { get; set; }
        [DataMember(Name = "last")]
        public int Last { get; set; }
        [DataMember(Name = "count")]
        public int Count { get; set; }
        [DataMember(Name = "page")]
        public int Page { get; set; }
        [DataMember(Name = "carrier")]
        public int Carrier { get; set; }
        [DataMember(Name = "GenreInformation")]
        public IEnumerable<object> GenreInformation { get; set; }
        [DataMember(Name = "first")]
        public int First { get; set; }
    }

    public class ComicInfo
    {
        [DataMember(Name = "Item")]
        public ComicInfos Info { get; set; }
    }

    public class ComicInfos
    {
        [DataMember(Name = "limitedFlag")]
        public int LimitedFlag { get; set; }
        [DataMember(Name = "authorKana")]
        public string AuthorKana { get; set; }
        [DataMember(Name = "author")]
        public string Author { get; set; }
        [DataMember(Name = "subTitle")]
        public string subTitle { get; set; }
        [DataMember(Name = "seriesNameKana")]
        public string SeriesNameKana { get; set; }
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "subTitleKana")]
        public string SubTitleKana { get; set; }
        [DataMember(Name = "itemCaption")]
        public string ItemCaption { get; set; }
        [DataMember(Name = "publisherName")]
        public string PublisherName { get; set; }
        [DataMember(Name = "listPrice")]
        public int ListPrice { get; set; }
        [DataMember(Name = "isbn")]
        public string Isbn { get; set; }
        [DataMember(Name = "largeImageUrl")]
        public string LargeImageUrl { get; set; }
        [DataMember(Name = "mediumImageUrl")]
        public string MediumImageUrl { get; set; }
        [DataMember(Name = "titleKana")]
        public string TitleKana { get; set; }
        [DataMember(Name = "availability")]
        public string Availability { get; set; }
        [DataMember(Name = "postageFlag")]
        public int PostageFlag { get; set; }
        [DataMember(Name = "salesDate")]
        public string SalesDate { get; set; }
        [DataMember(Name = "contents")]
        public string Contents { get; set; }
        [DataMember(Name = "smallImageUrl")]
        public string SmallImageUrl { get; set; }
        [DataMember(Name = "discountPrice")]
        public int DiscountPrice { get; set; }
        [DataMember(Name = "itemPrice")]
        public int ItemPrice { get; set; }
        [DataMember(Name = "size")]
        public string Size { get; set; }
        [DataMember(Name = "booksGenreId")]
        public string BooksGenreId { get; set; }
        [DataMember(Name = "affiliateUrl")]
        public string AffiliateUrl { get; set; }
        [DataMember(Name = "seriesName")]
        public string SeriesName { get; set; }
        [DataMember(Name = "reviewCount")]
        public int ReviewCount { get; set; }
        [DataMember(Name = "reviewAverage")]
        public string ReviewAverage { get; set; }
        [DataMember(Name = "discountRate")]
        public int DiscountRate { get; set; }
        [DataMember(Name = "chirayomiUrl")]
        public string ChirayomiUrl { get; set; }
        [DataMember(Name = "itemUrl")]
        public string ItemUrl { get; set; }
    }

}
