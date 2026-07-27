using ComiCal.Application.UseCases.Volumes;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class GetUpcomingVolumesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithKeywords_PassesKeywordsToRepository()
    {
        var repository = Substitute.For<IVolumeRepository>();
        repository.GetUpcomingAsync(Arg.Any<UpcomingQuery>(), Arg.Any<CancellationToken>())
            .Returns(([], (string?)null));
        var sut = new GetUpcomingVolumesUseCase(repository);
        var query = new UpcomingQuery(null, Keywords: ["作品名", "著者名"]);

        var result = await sut.ExecuteAsync(query, null);

        Assert.True(result.IsSuccess);
        await repository.Received(1).GetUpcomingAsync(query, Arg.Any<CancellationToken>());
    }
}
