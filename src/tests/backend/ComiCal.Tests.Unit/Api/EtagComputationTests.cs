using System.Text;
using ComiCal.Api.Common;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

public sealed class EtagComputationTests
{
    [Fact]
    public void ComputeWeakEtag_is_stable_for_identical_input()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"items\":[],\"nextCursor\":null}");

        var etag1 = EtagSupport.ComputeWeakEtag(bytes);
        var etag2 = EtagSupport.ComputeWeakEtag(bytes);

        etag1.Should().Be(etag2);
    }

    [Fact]
    public void ComputeWeakEtag_differs_when_input_differs()
    {
        var a = Encoding.UTF8.GetBytes("{\"items\":[1]}");
        var b = Encoding.UTF8.GetBytes("{\"items\":[2]}");

        EtagSupport.ComputeWeakEtag(a).Should().NotBe(EtagSupport.ComputeWeakEtag(b));
    }

    [Fact]
    public void ComputeWeakEtag_uses_weak_prefix_and_quoted_hex()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");

        var etag = EtagSupport.ComputeWeakEtag(bytes);

        etag.Should().StartWith("W/\"");
        etag.Should().EndWith("\"");
        // SHA-1 of "hello" = aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d (lowercase hex).
        etag.Should().Be("W/\"aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d\"");
    }

    [Fact]
    public void ComputeWeakEtag_produces_40_hex_chars()
    {
        var bytes = Encoding.UTF8.GetBytes("payload");

        var etag = EtagSupport.ComputeWeakEtag(bytes);

        // W/" + 40 hex + "
        etag.Length.Should().Be(3 + 40 + 1);
    }
}
