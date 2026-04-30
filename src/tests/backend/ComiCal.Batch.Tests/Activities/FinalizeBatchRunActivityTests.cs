using ComiCal.Batch.Activities;
using ComiCal.Batch.Models;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Activities;

public sealed class FinalizeBatchRunActivityTests
{
    private readonly IBatchRunRepository _batchRunRepo = Substitute.For<IBatchRunRepository>();
    private readonly TaskActivityContext _context = Substitute.For<TaskActivityContext>();
    private readonly FinalizeBatchRunActivity _sut;

    public FinalizeBatchRunActivityTests()
    {
        _sut = new FinalizeBatchRunActivity(
            _batchRunRepo,
            Substitute.For<ILogger<FinalizeBatchRunActivity>>());
    }

    [Fact]
    public async Task RunAsync_BatchRunFound_CallsCompleteAndUpdatesRepository()
    {
        // Arrange
        var batchRunId = Guid.NewGuid();
        var batchRun = BatchRun.Create();
        _batchRunRepo.FindByIdAsync(batchRunId, Arg.Any<CancellationToken>()).Returns(batchRun);
        var input = new FinalizeBatchRunInput(batchRunId, 100, 80, 70, 10, 5, true);

        // Act
        var result = await _sut.RunAsync(_context, input);

        // Assert
        Assert.True(result);
        Assert.Equal(100, batchRun.FetchedItemCount);
        Assert.Equal(80, batchRun.UpsertedVolumeCount);
        Assert.Equal(70, batchRun.DownloadedThumbnailCount);
        await _batchRunRepo.Received(1).UpdateAsync(batchRun, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithFailedItems_StatusSetToFailed()
    {
        var batchRunId = Guid.NewGuid();
        var batchRun = BatchRun.Create();
        _batchRunRepo.FindByIdAsync(batchRunId, Arg.Any<CancellationToken>()).Returns(batchRun);

        await _sut.RunAsync(_context, new FinalizeBatchRunInput(batchRunId, 50, 40, 30, 5, 3, true));

        Assert.Equal(BatchRunStatus.Failed, batchRun.Status);
    }

    [Fact]
    public async Task RunAsync_WithNoFailedItems_StatusSetToSucceeded()
    {
        var batchRunId = Guid.NewGuid();
        var batchRun = BatchRun.Create();
        _batchRunRepo.FindByIdAsync(batchRunId, Arg.Any<CancellationToken>()).Returns(batchRun);

        await _sut.RunAsync(_context, new FinalizeBatchRunInput(batchRunId, 50, 50, 45, 5, 0, true));

        Assert.Equal(BatchRunStatus.Succeeded, batchRun.Status);
        Assert.NotNull(batchRun.CompletedAt);
    }

    [Fact]
    public async Task RunAsync_BatchRunNotFound_ReturnsFalseWithoutUpdate()
    {
        // Arrange
        var batchRunId = Guid.NewGuid();
        _batchRunRepo.FindByIdAsync(batchRunId, Arg.Any<CancellationToken>()).Returns((BatchRun?)null);

        // Act
        var result = await _sut.RunAsync(_context, new FinalizeBatchRunInput(batchRunId, 0, 0, 0, 0, 0, true));

        // Assert
        Assert.False(result);
        await _batchRunRepo.DidNotReceive().UpdateAsync(Arg.Any<BatchRun>(), Arg.Any<CancellationToken>());
    }
}
