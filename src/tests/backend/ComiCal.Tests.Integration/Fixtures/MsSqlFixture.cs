using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Testcontainers.MsSql;
using Xunit;

namespace ComiCal.Tests.Integration.Fixtures;

public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ApplyDacpacIfPresent();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private void ApplyDacpacIfPresent()
    {
        var dacpacPath = Path.Combine(System.AppContext.BaseDirectory, "ComiCal.Database.dacpac");
        if (!File.Exists(dacpacPath))
        {
            return;
        }

        using var package = DacPackage.Load(dacpacPath);
        var services = new DacServices(ConnectionString);
        services.Deploy(
            package,
            "ComiCal",
            upgradeExisting: true,
            options: new DacDeployOptions
            {
                CreateNewDatabase = true,
                BlockOnPossibleDataLoss = false,
            });
    }
}
