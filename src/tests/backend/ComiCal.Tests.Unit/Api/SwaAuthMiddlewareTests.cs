using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

public sealed class SwaAuthMiddlewareTests
{
    private static string EncodePrincipal(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    [Fact]
    public void DecodePrincipalHeader_returns_null_for_null_or_blank_header()
    {
        SwaAuthMiddleware.DecodePrincipalHeader(null).Should().BeNull();
        SwaAuthMiddleware.DecodePrincipalHeader(string.Empty).Should().BeNull();
        SwaAuthMiddleware.DecodePrincipalHeader("   ").Should().BeNull();
    }

    [Fact]
    public void DecodePrincipalHeader_extracts_principal_with_roles_and_claims()
    {
        var encoded = EncodePrincipal(new
        {
            identityProvider = "aadb2c",
            userId = "abc-123",
            userDetails = "alice@example.jp",
            userRoles = new[] { "anonymous", "authenticated" },
            claims = new[]
            {
                new { typ = "name", val = "Alice" },
                new { typ = "emails", val = "alice@example.jp" },
            },
        });

        var p = SwaAuthMiddleware.DecodePrincipalHeader(encoded);

        p.Should().NotBeNull();
        p!.IdentityProvider.Should().Be("aadb2c");
        p.UserId.Should().Be("abc-123");
        p.UserDetails.Should().Be("alice@example.jp");
        p.UserRoles.Should().Contain(new[] { "anonymous", "authenticated" });
        p.IsAuthenticated.Should().BeTrue();
        p.Claims.Should().HaveCount(2);
        p.Claims.Single(c => c.Type == "name").Value.Should().Be("Alice");
    }

    [Fact]
    public void DecodePrincipalHeader_unauth_when_authenticated_role_missing()
    {
        var encoded = EncodePrincipal(new
        {
            identityProvider = "aadb2c",
            userId = "abc-123",
            userDetails = "alice",
            userRoles = new[] { "anonymous" },
            claims = Array.Empty<object>(),
        });

        var p = SwaAuthMiddleware.DecodePrincipalHeader(encoded);

        p.Should().NotBeNull();
        p!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void DecodePrincipalHeader_throws_FormatException_for_non_base64()
    {
        Action act = () => SwaAuthMiddleware.DecodePrincipalHeader("@@@not-base64@@@");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void DecodePrincipalHeader_throws_JsonException_for_non_json()
    {
        // Valid base64 of plaintext "hello" — not JSON.
        var bad = Convert.ToBase64String(Encoding.UTF8.GetBytes("hello"));
        Action act = () => SwaAuthMiddleware.DecodePrincipalHeader(bad);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Anonymous_principal_is_not_authenticated()
    {
        ClientPrincipal.Anonymous.IsAuthenticated.Should().BeFalse();
        ClientPrincipal.Anonymous.UserRoles.Should().Contain("anonymous");
    }

    // -------------------- RequiresAuthenticatedUserAttribute marker --------------------

    private sealed class GuardedFunction
    {
        [RequiresAuthenticatedUser]
        public void Run() { }
    }

    [RequiresAuthenticatedUser]
    private sealed class TypeGuardedFunction
    {
        public void Run() { }
    }

    private sealed class OpenFunction
    {
        public void Run() { }
    }

    [Fact]
    public void TypeOrMethodRequiresAuth_detects_method_attribute()
    {
        SwaAuthMiddleware.TypeOrMethodRequiresAuth(typeof(GuardedFunction), nameof(GuardedFunction.Run))
            .Should().BeTrue();
    }

    [Fact]
    public void TypeOrMethodRequiresAuth_detects_class_attribute()
    {
        SwaAuthMiddleware.TypeOrMethodRequiresAuth(typeof(TypeGuardedFunction), nameof(TypeGuardedFunction.Run))
            .Should().BeTrue();
    }

    [Fact]
    public void TypeOrMethodRequiresAuth_returns_false_when_unmarked()
    {
        SwaAuthMiddleware.TypeOrMethodRequiresAuth(typeof(OpenFunction), nameof(OpenFunction.Run))
            .Should().BeFalse();
    }

    // -------------------- CurrentUserAccessor --------------------

    [Fact]
    public void CurrentUserAccessor_defaults_to_anonymous()
    {
        var accessor = new CurrentUserAccessor();
        accessor.Principal.Should().BeSameAs(ClientPrincipal.Anonymous);
        accessor.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void CurrentUserAccessor_reflects_authenticated_principal()
    {
        var accessor = new CurrentUserAccessor
        {
            Principal = new ClientPrincipal(
                IdentityProvider: "aadb2c",
                UserId: "u1",
                UserDetails: "u",
                UserRoles: new[] { "authenticated" },
                Claims: Array.Empty<ClientPrincipalClaim>()),
        };

        accessor.IsAuthenticated.Should().BeTrue();
        accessor.Principal.UserId.Should().Be("u1");
    }
}
