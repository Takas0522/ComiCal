namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// Rakuten Books API の認証情報を保持します。
/// </summary>
public sealed class RakutenAuthCredentials
{
    public string ApplicationId { get; }
    public string AccessKey { get; }
    public string AffiliateId { get; }

    public RakutenAuthCredentials(string applicationId, string accessKey, string affiliateId)
    {
        ApplicationId = applicationId;
        AccessKey = accessKey;
        AffiliateId = affiliateId;
    }
}
