using System.Reflection;
using ComiCal.Api.Common;
using ComiCal.Api.Functions;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

/// <summary>
/// Reflection-level guarantees that the Phase 2 <c>/api/me/*</c> Function classes
/// are gated on authentication. The actual 401 behaviour is exercised by
/// <see cref="SwaAuthMiddlewareTests"/>.
/// </summary>
public sealed class MeFunctionsAuthorizationTests
{
    [Theory]
    [InlineData(typeof(MeSubscriptionsFunctions))]
    [InlineData(typeof(MePurchasesFunctions))]
    [InlineData(typeof(MeAccountFunctions))]
    [InlineData(typeof(MeSyncMergeFunctions))]
    [InlineData(typeof(MeSyncFunctions))]
    public void Class_is_decorated_with_RequiresAuthenticatedUser(Type functionClass)
    {
        functionClass.GetCustomAttribute<RequiresAuthenticatedUserAttribute>(inherit: true)
            .Should().NotBeNull(
                "all /api/me/* function classes must opt into SWA auth gating");
    }

    [Theory]
    [InlineData(typeof(MeSubscriptionsFunctions))]
    [InlineData(typeof(MePurchasesFunctions))]
    [InlineData(typeof(MeAccountFunctions))]
    [InlineData(typeof(MeSyncMergeFunctions))]
    [InlineData(typeof(MeSyncFunctions))]
    public void Class_depends_on_ICurrentUser_via_constructor(Type functionClass)
    {
        var ctor = functionClass.GetConstructors().Single();
        ctor.GetParameters()
            .Should().Contain(p => p.ParameterType == typeof(ICurrentUser),
                "user identity must come from ICurrentUser, never from the request body");
    }
}
