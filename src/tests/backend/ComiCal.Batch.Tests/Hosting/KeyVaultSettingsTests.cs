using ComiCal.Batch.Hosting;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class KeyVaultSettingsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadAsync_ConfiguredVault_MapsNamesAndPreservesExplicitSettings(bool overrides)
    {
        var uri = new Uri("https://test.vault.azure.net/");
        var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:KeyVaultUri"] = uri.AbsoluteUri,
            ["RakutenApplicationId"] = ""
        });
        if (overrides)
        {
            builder.AddJsonStream(new MemoryStream("""{"RakutenApplicationId":"local","RakutenAccessKey":"local"}"""u8.ToArray()))
                .AddInMemoryCollection(new Dictionary<string, string?> { ["RakutenApplicationId"] = "environment" });
        }
        var config = builder.Build();
        var source = Substitute.For<IBatchSecretSource>();
        source.GetAsync(uri, "RakutenAppId", default).Returns("app");
        source.GetAsync(uri, "RakutenAccessKey", default).Returns("access");
        source.GetAsync(uri, "RakutenAffiliateId", default).Returns("affiliate");
        source.GetAsync(uri, "AppInsightsConnectionString", default).Returns(overrides ? null : "insights");

        await new KeyVaultSettings(config, source).LoadAsync(default);

        Assert.Equal(overrides ? "environment" : "app", config["RakutenApplicationId"]);
        Assert.Equal(overrides ? "local" : "access", config["RakutenAccessKey"]);
        Assert.Equal("affiliate", config["RakutenAffiliateId"]);
        Assert.Equal(overrides ? null : "insights", config["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
        await source.Received(overrides ? 0 : 1).GetAsync(uri, "RakutenAppId", default);
        await source.Received(overrides ? 0 : 1).GetAsync(uri, "RakutenAccessKey", default);
    }
}
