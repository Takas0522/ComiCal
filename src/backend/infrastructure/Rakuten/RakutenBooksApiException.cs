namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// 楽天 Books API 呼び出しがリトライ尽きで失敗した際にスローされる。
/// </summary>
public sealed class RakutenBooksApiException : Exception
{
    public int? StatusCode { get; }

    public RakutenBooksApiException(string message)
        : base(message)
    {
    }

    public RakutenBooksApiException(string message, int? statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
