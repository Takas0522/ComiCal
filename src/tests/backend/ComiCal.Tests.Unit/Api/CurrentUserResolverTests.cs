using System;
using System.Threading;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

public sealed class CurrentUserResolverTests
{
    private static ClientPrincipal AuthenticatedPrincipal(string userId = "ext-abc-123", string? userDetails = "alice@example.jp")
        => new(
            IdentityProvider: "aadb2c",
            UserId: userId,
            UserDetails: userDetails ?? string.Empty,
            UserRoles: new[] { "anonymous", "authenticated" },
            Claims: Array.Empty<ClientPrincipalClaim>());

    [Fact]
    public async Task Anonymous_principal_skips_repository_call_and_leaves_accessor_empty()
    {
        var repo = Substitute.For<IUserRepository>();
        var current = new CurrentUser();

        await CurrentUserResolverMiddleware.ResolveAsync(
            ClientPrincipal.Anonymous,
            repo,
            current,
            NullLogger.Instance,
            CancellationToken.None);

        current.IsAuthenticated.Should().BeFalse();
        current.Id.Should().Be(Guid.Empty);
        current.ExternalId.Should().BeEmpty();
        await repo.DidNotReceive().EnsureExistsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().GetByExternalIdAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Existing_user_is_returned_and_accessor_is_populated()
    {
        var existingId = Guid.CreateVersion7();
        var existing = User.Hydrate(
            id: existingId,
            externalId: "ext-abc-123",
            displayName: "Alice",
            role: "User",
            isDeleted: false,
            deletedAt: null,
            createdAt: DateTime.UtcNow.AddDays(-30),
            updatedAt: DateTime.UtcNow.AddDays(-30));

        var repo = Substitute.For<IUserRepository>();
        repo.EnsureExistsAsync("ext-abc-123", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        var current = new CurrentUser();

        await CurrentUserResolverMiddleware.ResolveAsync(
            AuthenticatedPrincipal("ext-abc-123", "alice@example.jp"),
            repo,
            current,
            NullLogger.Instance,
            CancellationToken.None);

        current.IsAuthenticated.Should().BeTrue();
        current.Id.Should().Be(existingId);
        current.ExternalId.Should().Be("ext-abc-123");
        current.DisplayName.Should().Be("Alice");
        await repo.Received(1).EnsureExistsAsync(
            "ext-abc-123", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task New_user_path_invokes_upsert_with_resolved_display_name()
    {
        var newId = Guid.CreateVersion7();
        var captured = User.CreateNew(newId, "ext-new-999", "alice@example.jp");

        var repo = Substitute.For<IUserRepository>();
        string? observedExternalId = null;
        string? observedDisplayName = null;
        repo.EnsureExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedExternalId = call.ArgAt<string>(0);
                observedDisplayName = call.ArgAt<string>(1);
                return Task.FromResult(captured);
            });
        var current = new CurrentUser();

        await CurrentUserResolverMiddleware.ResolveAsync(
            AuthenticatedPrincipal("ext-new-999", "alice@example.jp"),
            repo,
            current,
            NullLogger.Instance,
            CancellationToken.None);

        observedExternalId.Should().Be("ext-new-999");
        observedDisplayName.Should().Be("alice@example.jp");
        current.Id.Should().Be(newId);
        current.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task Display_name_falls_back_to_user_id_when_user_details_blank()
    {
        var newId = Guid.CreateVersion7();
        var captured = User.CreateNew(newId, "ext-no-name", "ext-no-name");

        var repo = Substitute.For<IUserRepository>();
        string? observedDisplayName = null;
        repo.EnsureExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedDisplayName = call.ArgAt<string>(1);
                return Task.FromResult(captured);
            });

        await CurrentUserResolverMiddleware.ResolveAsync(
            AuthenticatedPrincipal("ext-no-name", userDetails: null),
            repo,
            new CurrentUser(),
            NullLogger.Instance,
            CancellationToken.None);

        observedDisplayName.Should().Be("ext-no-name");
    }

    [Fact]
    public void Display_name_is_truncated_to_64_chars()
    {
        var longName = new string('あ', 100);
        var p = new ClientPrincipal(
            IdentityProvider: "aadb2c",
            UserId: "ext-1",
            UserDetails: longName,
            UserRoles: new[] { "authenticated" },
            Claims: Array.Empty<ClientPrincipalClaim>());

        var resolved = CurrentUserResolverMiddleware.ResolveDisplayName(p);

        resolved.Length.Should().Be(64);
    }

    [Fact]
    public void CurrentUser_defaults_to_unauthenticated_empty_state()
    {
        var u = new CurrentUser();
        u.IsAuthenticated.Should().BeFalse();
        u.Id.Should().Be(Guid.Empty);
        u.ExternalId.Should().BeEmpty();
        u.DisplayName.Should().BeEmpty();
    }

    [Fact]
    public void CurrentUser_Populate_rejects_empty_id()
    {
        var u = new CurrentUser();
        Action act = () => u.Populate(Guid.Empty, "ext", "n");
        act.Should().Throw<ArgumentException>();
    }
}
