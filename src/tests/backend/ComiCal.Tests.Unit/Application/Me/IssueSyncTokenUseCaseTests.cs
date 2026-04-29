using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class IssueSyncTokenUseCaseTests
{
    private readonly ISyncTokenRepository _repo = Substitute.For<ISyncTokenRepository>();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-04-29T12:00:00Z"));

    private IssueSyncTokenUseCase Sut => new(_repo, NullLogger<IssueSyncTokenUseCase>.Instance, _time);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new IssueSyncTokenCommand("https://comical.example"),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Returns_validation_when_qr_base_url_blank()
    {
        var result = await Sut.ExecuteAsync(
            new IssueSyncTokenCommand("   "),
            new UseCaseContext(Guid.CreateVersion7(), "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Issues_token_persists_hash_and_returns_qr_payload()
    {
        var userId = Guid.CreateVersion7();
        SyncToken? captured = null;
        await _repo.AddAsync(Arg.Do<SyncToken>(t => captured = t), Arg.Any<CancellationToken>());

        var result = await Sut.ExecuteAsync(
            new IssueSyncTokenCommand("https://comical.example/"),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.Token.Should().NotBeNullOrWhiteSpace();
        dto.Token.Length.Should().BeGreaterThan(40);
        // base64url alphabet only
        dto.Token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        dto.QrPayload.Should().StartWith("https://comical.example/sync?token=");
        dto.ExpiresAt.Should().Be(_time.GetUtcNow().UtcDateTime + IssueSyncTokenUseCase.DefaultTtl);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.TokenHash.Should().HaveCount(32);
        captured.TokenHash.Should().BeEquivalentTo(SyncToken.ComputeHash(dto.Token));
        captured.ConsumedAt.Should().BeNull();
    }
}
