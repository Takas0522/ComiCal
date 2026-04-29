using ComiCal.Domain.ValueObjects;

namespace ComiCal.Domain.DomainServices;

public interface IIsbnNormalizer
{
    Isbn13 Normalize(string raw);
}
