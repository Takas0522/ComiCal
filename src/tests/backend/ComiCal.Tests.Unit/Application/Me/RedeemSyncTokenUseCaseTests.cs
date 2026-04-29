using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Application.Validators;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class RedeemSyncTokenUseCaseTests
{
    private readonly IValidator<RedeemSyncTokenCommand> _validator = new RedeemSyncTokenCommandValidator();
    private readonly ISyncTokenRepository _repo = Substitute.For<ISyncTokenRepository>();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-04-29T12:00:00Z"));

    private RedeemSyncTokenUseCase Sut => new(_validator, _repo, NullLogger<RedeemSyncTokenUseCase>.Instance, _time);

    private static (SyncToken Entity, string Plain) NewToken(Guid userId, DateTime nowUtc, TimeSpan? ttl = null)
        => SyncToken.CreateNew(Guid.CreateVersion7(), userId, nowUtc, ttl ?? TimeSpan.FromMinutes(5));

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand("anytoken"),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Returns_validation_when_token_blank()
    {
        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand(string.Empty),
            new UseCaseContext(Guid.CreateVersion7(), "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Returns_not_found_when_token_missing()
    {
        var userId = Guid.CreateVersion7();
        _repo.FindByHashAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns((SyncToken?)null);

        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand("not-a-real-token"),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Code.Should().Be("sync-token-not-found");
    }

    [Fact]
    public async Task Returns_already_consumed_when_consumed_at_set()
    {
        var userId = Guid.CreateVersion7();
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var (entity, plain) = NewToken(userId, nowUtc);
        entity.MarkConsumed(nowUtc);
        _repo.FindByHashAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(entity);

        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand(plain),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        result.Error.Code.Should().Be("sync-token-already-consumed");
    }

    [Fact]
    public async Task Returns_expired_when_past_expires_at()
    {
        var userId = Guid.CreateVersion7();
        var issuedAt = _time.GetUtcNow().UtcDateTime;
        var (entity, plain) = NewToken(userId, issuedAt, TimeSpan.FromMinutes(5));
        _time.Advance(TimeSpan.FromMinutes(6));
        _repo.FindByHashAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(entity);

        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand(plain),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Code.Should().Be("sync-token-expired");
    }

    [Fact]
    public async Task Returns_user_mismatch_when_token_belongs_to_other_user()
    {
        var issuer = Guid.CreateVersion7();
        var redeemer = Guid.CreateVersion7();
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var (entity, plain) = NewToken(issuer, nowUtc);
        _repo.FindByHashAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(entity);

        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand(plain),
            new UseCaseContext(redeemer, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Forbidden);
        result.Error.Code.Should().Be("sync-token-user-mismatch");
        await _repo.DidNotReceiveWithAnyArgs().MarkConsumedAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Marks_consumed_on_happy_path()
    {
        var userId = Guid.CreateVersion7();
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var (entity, plain) = NewToken(userId, nowUtc);
        _repo.FindByHashAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(entity);
        _repo.MarkConsumedAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand(plain),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).MarkConsumedAsync(entity.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_already_consumed_when_mark_loses_race()
    {
        var userId = Guid.CreateVersion7();
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var (entity, plain) = NewToken(userId, nowUtc);
        _repo.FindByHashAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(entity);
        _repo.MarkConsumedAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut.ExecuteAsync(
            new RedeemSyncTokenCommand(plain),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("sync-token-already-consumed");
    }
}
