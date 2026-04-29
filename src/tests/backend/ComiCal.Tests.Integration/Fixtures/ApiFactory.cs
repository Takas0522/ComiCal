using Xunit;

namespace ComiCal.Tests.Integration.Fixtures;

// NOTE: WebApplicationFactory<Program> is non-trivial for Azure Functions Isolated Worker
// because Program.cs uses FunctionsApplication.CreateBuilder rather than a generic host.
// This collection groups tests that share MSSQL + Azurite container state to avoid
// container thrash. Full HTTP-level Functions hosting will be wired in a later stage.
[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<MsSqlFixture>, ICollectionFixture<AzuriteFixture>
{
    public const string Name = "ApiIntegration";
}
