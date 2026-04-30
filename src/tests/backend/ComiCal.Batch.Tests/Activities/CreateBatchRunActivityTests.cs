using ComiCal.Batch.Activities;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.DurableTask;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Activities;

public sealed class CreateBatchRunActivityTests
{
    private readonly IBatchRunRepository _batchRunRepo = Substitute.For<IBatchRunRepository>();
    private readonly TaskActivityContext _context = Substitute.For<TaskActivityContext>();
    private readonly CreateBatchRunActivity _sut;

    public CreateBatchRunActivityTests()
    {
        _sut = new CreateBatchRunActivity(_batchRunRepo);
    }

    [Fact]
    public async Task RunAsync_CallsRepositoryCreate_ReturnsBatchRunId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        _batchRunRepo.CreateAsync(Arg.Any<BatchRun>(), Arg.Any<CancellationToken>())
            .Returns(expectedId);

        // Act
        var result = await _sut.RunAsync(_context, null);

        // Assert
        Assert.Equal(expectedId, result);
        await _batchRunRepo.Received(1).CreateAsync(Arg.Any<BatchRun>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PassesFreshBatchRunWithNonEmptyId()
    {
        // Arrange
        BatchRun? captured = null;
        _batchRunRepo
            .CreateAsync(Arg.Do<BatchRun>(br => captured = br), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        // Act
        await _sut.RunAsync(_context, null);

        // Assert — BatchRun.Create() always generates a non-empty GUID
        Assert.NotNull(captured);
        Assert.NotEqual(Guid.Empty, captured.BatchRunId);
    }
}
