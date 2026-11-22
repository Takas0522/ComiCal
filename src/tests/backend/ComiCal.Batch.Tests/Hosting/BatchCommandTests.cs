using ComiCal.Batch.Hosting;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class BatchCommandTests
{
    [Fact]
    public void Parse_SupportedCommands_ReturnsOptions()
    {
        Assert.Equal(new BatchCommand(BatchCommandKind.Run), BatchCommand.Parse([]));
        Assert.Equal(new BatchCommand(BatchCommandKind.Run), BatchCommand.Parse(["run"]));
        Assert.Equal(new BatchCommand(BatchCommandKind.Run, true), BatchCommand.Parse(["run", "--force"]));
        Assert.Equal(new BatchCommand(BatchCommandKind.Status), BatchCommand.Parse(["status"]));
        Assert.Equal(new BatchCommand(BatchCommandKind.Status, Date: new(2026, 11, 21)),
            BatchCommand.Parse(["status", "--date", "2026-11-21"]));
        Assert.Equal(new BatchCommand(BatchCommandKind.Preflight), BatchCommand.Parse(["preflight"]));
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("run --date")]
    [InlineData("run --force --force")]
    [InlineData("status --date")]
    [InlineData("status --date 2026-02-29")]
    [InlineData("status --date 2026-1-01")]
    [InlineData("preflight --force")]
    public void Parse_InvalidArguments_ReturnsNull(string args)
        => Assert.Null(BatchCommand.Parse(args.Split(' ')));
}
