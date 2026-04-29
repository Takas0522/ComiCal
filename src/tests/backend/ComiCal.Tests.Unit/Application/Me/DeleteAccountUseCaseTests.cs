using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class DeleteAccountUseCaseTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private DeleteAccountUseCase Sut => new(_repo);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing_from_context()
    {
        var result = await Sut.ExecuteAsync(
            new DeleteAccountCommand(Guid.CreateVersion7()),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
        await _repo.DidNotReceiveWithAnyArgs().HardDeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_unauthorized_when_command_user_id_does_not_match_context()
    {
        var contextUserId = Guid.CreateVersion7();
        var attackerUserId = Guid.CreateVersion7();

        var result = await Sut.ExecuteAsync(
            new DeleteAccountCommand(attackerUserId),
            new UseCaseContext(contextUserId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
        await _repo.DidNotReceiveWithAnyArgs().HardDeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Hard_deletes_user_and_returns_success_on_happy_path()
    {
        var userId = Guid.CreateVersion7();
        _repo.HardDeleteAsync(userId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut.ExecuteAsync(
            new DeleteAccountCommand(userId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).HardDeleteAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_success_when_user_already_absent_idempotent()
    {
        var userId = Guid.CreateVersion7();
        _repo.HardDeleteAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut.ExecuteAsync(
            new DeleteAccountCommand(userId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue("missing user is treated as already-deleted (idempotent)");
        await _repo.Received(1).HardDeleteAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_repository_exception_so_middleware_renders_500()
    {
        var userId = Guid.CreateVersion7();
        _repo.HardDeleteAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var act = () => Sut.ExecuteAsync(
            new DeleteAccountCommand(userId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB down");
    }
}
