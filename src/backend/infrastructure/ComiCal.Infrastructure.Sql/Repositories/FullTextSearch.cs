using ComiCal.Domain.DomainServices;

namespace ComiCal.Infrastructure.Sql.Repositories;

internal static class FullTextSearch
{
    public static string NormalizePhrase(string phrase)
        => string.IsNullOrWhiteSpace(phrase) ? string.Empty : TitleNormalizer.Normalize(phrase);
}
