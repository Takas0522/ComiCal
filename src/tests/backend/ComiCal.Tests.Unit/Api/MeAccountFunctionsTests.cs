using System.Reflection;
using ComiCal.Api.Common;
using ComiCal.Api.Functions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

/// <summary>
/// Reflection-level guarantees for <see cref="MeAccountFunctions"/>: the route,
/// HTTP verb, authorization level and auth gating are part of the public
/// contract and must not regress (a stray edit could downgrade auth or open
/// the endpoint to anonymous callers).
/// </summary>
public sealed class MeAccountFunctionsTests
{
    [Fact]
    public void Class_requires_authenticated_user()
    {
        typeof(MeAccountFunctions)
            .GetCustomAttribute<RequiresAuthenticatedUserAttribute>(inherit: true)
            .Should().NotBeNull("DELETE /api/me/account must be SWA-gated");
    }

    [Fact]
    public void Constructor_accepts_ICurrentUser_so_user_id_never_comes_from_body()
    {
        var ctor = typeof(MeAccountFunctions).GetConstructors().Single();
        ctor.GetParameters().Should().Contain(p => p.ParameterType == typeof(ICurrentUser));
    }

    [Fact]
    public void DeleteAsync_is_a_function_with_correct_route_and_verb()
    {
        var method = typeof(MeAccountFunctions)
            .GetMethod(nameof(MeAccountFunctions.DeleteAsync), BindingFlags.Public | BindingFlags.Instance)!;

        var fn = method.GetCustomAttribute<FunctionAttribute>(inherit: true);
        fn.Should().NotBeNull();
        fn!.Name.Should().Be("DeleteMeAccount");

        var trigger = method.GetParameters()
            .Single(p => p.GetCustomAttribute<HttpTriggerAttribute>(inherit: true) is not null)
            .GetCustomAttribute<HttpTriggerAttribute>(inherit: true)!;

        trigger.AuthLevel.Should().Be(AuthorizationLevel.Function,
            "SWA-linked Functions must require an authentication header at the platform level");
        trigger.Methods.Should().BeEquivalentTo(["delete"]);
        trigger.Route.Should().Be("me/account");
    }

    [Fact]
    public void Logout_required_header_constant_is_stable()
    {
        // Frontend depends on this exact header name to drive the SWA logout
        // redirect after a successful 204.
        MeAccountFunctions.LogoutRequiredHeader.Should().Be("X-Logout-Required");
    }
}
