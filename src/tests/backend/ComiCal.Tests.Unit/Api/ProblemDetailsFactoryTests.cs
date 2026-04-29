using ComiCal.Api.ProblemDetails;
using ComiCal.Shared;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

public sealed class ProblemDetailsFactoryTests
{
    private readonly ProblemDetailsFactory _factory = new();

    [Fact]
    public void FromError_validation_maps_to_400_with_problem_type_uri()
    {
        var error = Error.Validation("limit-out-of-range", "limit must be 1..100");

        var body = _factory.FromError(error, instance: "/api/series", traceId: "trace-1");

        body.Status.Should().Be(400);
        body.Type.Should().Be("https://comical.example.com/problems/limit-out-of-range");
        body.Title.Should().Be("Validation failed");
        body.Detail.Should().Be("limit must be 1..100");
        body.Instance.Should().Be("/api/series");
        body.TraceId.Should().Be("trace-1");
        body.Errors.Should().BeNull();
    }

    [Fact]
    public void FromError_notfound_maps_to_404()
    {
        var error = Error.NotFound("series-not-found", "Series '...' was not found.");

        var body = _factory.FromError(error, instance: null, traceId: null);

        body.Status.Should().Be(404);
        body.Type.Should().Be("https://comical.example.com/problems/series-not-found");
        body.Title.Should().Be("Not found");
    }

    [Fact]
    public void FromError_conflict_maps_to_409()
    {
        var error = Error.Conflict("duplicate", "already exists");

        var body = _factory.FromError(error, instance: null, traceId: null);

        body.Status.Should().Be(409);
    }

    [Fact]
    public void FromError_unexpected_maps_to_500()
    {
        var error = Error.Unexpected("boom", "kaboom");

        var body = _factory.FromError(error, instance: null, traceId: null);

        body.Status.Should().Be(500);
        body.Type.Should().Be("https://comical.example.com/problems/internal-error");
    }

    [Fact]
    public void FromValidation_groups_failures_by_property_name()
    {
        var failures = new[]
        {
            new ValidationFailure("Limit", "must be > 0"),
            new ValidationFailure("Limit", "must be <= 100"),
            new ValidationFailure("Cursor", "is malformed"),
        };

        var body = _factory.FromValidation(failures, instance: "/api/series", traceId: "abc");

        body.Status.Should().Be(400);
        body.Type.Should().Be("https://comical.example.com/problems/validation");
        body.Errors.Should().NotBeNull();
        body.Errors!["Limit"].Should().BeEquivalentTo("must be > 0", "must be <= 100");
        body.Errors!["Cursor"].Should().BeEquivalentTo("is malformed");
    }

    [Fact]
    public void RateLimited_returns_429_problem()
    {
        var body = _factory.RateLimited(instance: "/api/series", traceId: "t");

        body.Status.Should().Be(429);
        body.Type.Should().Be("https://comical.example.com/problems/rate-limited");
        body.Title.Should().Be("Too many requests");
    }

    [Fact]
    public void Unhandled_omits_detail_when_null()
    {
        var body = _factory.Unhandled(detail: null, instance: null, traceId: null);

        body.Status.Should().Be(500);
        body.Detail.Should().BeNull();
    }
}
