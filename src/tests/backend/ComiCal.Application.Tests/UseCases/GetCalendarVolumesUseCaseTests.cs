using ComiCal.Application.UseCases.Volumes;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class GetCalendarVolumesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRepositoryReturnsDatedAndUndatedMatches_SeparatesThem()
    {
        var repository = Substitute.For<IVolumeRepository>();
        var seriesId = Guid.NewGuid();
        var dated = Volume.Create(seriesId, "9784000000001", releaseDate: new DateTime(2026, 8, 1));
        var undated = Volume.Create(seriesId, "9784000000002");
        repository.GetCalendarAsync(Arg.Any<CalendarQuery>(), Arg.Any<CancellationToken>())
            .Returns([dated, undated]);
        var sut = new GetCalendarVolumesUseCase(repository);
        var query = new CalendarQuery(2026, 8, Keywords: ["作品名"]);

        var result = await sut.ExecuteAsync(query, null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Days);
        Assert.Single(result.Value.UndatedVolumes);
        Assert.Equal("9784000000002", result.Value.UndatedVolumes[0].Isbn13);
        await repository.Received(1).GetCalendarAsync(query, Arg.Any<CancellationToken>());
    }
}
