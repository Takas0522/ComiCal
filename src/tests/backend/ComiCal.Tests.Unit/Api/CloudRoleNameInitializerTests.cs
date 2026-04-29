using ComiCal.Api.Common;
using ComiCal.Api.Observability;
using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ComiCal.Tests.Unit.Api;

/// <summary>
/// Unit tests for <see cref="CloudRoleNameInitializer"/>. We validate behaviour we own
/// (role name + UserId stamping); we do <b>not</b> test the AI SDK pipeline itself.
/// </summary>
public sealed class CloudRoleNameInitializerTests
{
    [Fact]
    public void Initialize_sets_cloud_role_name_to_comical_api()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var initializer = new CloudRoleNameInitializer(services);
        var telemetry = new TraceTelemetry("hello");

        initializer.Initialize(telemetry);

        telemetry.Context.Cloud.RoleName.Should().Be("comical-api");
    }

    [Fact]
    public void Initialize_does_not_overwrite_existing_role_name()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var initializer = new CloudRoleNameInitializer(services);
        var telemetry = new TraceTelemetry("hello");
        telemetry.Context.Cloud.RoleName = "preset-role";

        initializer.Initialize(telemetry);

        telemetry.Context.Cloud.RoleName.Should().Be("preset-role");
    }

    [Fact]
    public void Initialize_stamps_UserId_when_authenticated_user_is_resolved()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICurrentUser>(_ =>
        {
            var u = new CurrentUser();
            u.Populate(Guid.Parse("11111111-2222-3333-4444-555555555555"), "ext-user-1", "Alice");
            return u;
        });
        var provider = services.BuildServiceProvider();

        var initializer = new CloudRoleNameInitializer(provider);
        var telemetry = new TraceTelemetry("hello");

        initializer.Initialize(telemetry);

        telemetry.Properties.Should().ContainKey("UserId")
            .WhoseValue.Should().Be("11111111-2222-3333-4444-555555555555");
    }

    [Fact]
    public void Initialize_omits_UserId_when_anonymous()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICurrentUser>(_ => new CurrentUser());
        var provider = services.BuildServiceProvider();

        var initializer = new CloudRoleNameInitializer(provider);
        var telemetry = new TraceTelemetry("hello");

        initializer.Initialize(telemetry);

        telemetry.Properties.Should().NotContainKey("UserId");
    }
}
