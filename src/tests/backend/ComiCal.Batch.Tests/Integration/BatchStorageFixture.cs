using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using Testcontainers.Azurite;
using Testcontainers.MsSql;
using Xunit;

namespace ComiCal.Batch.Tests.Integration;

public sealed class BatchStorageFixture : IAsyncLifetime
{
    private readonly AzuriteContainer blob = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
        .WithCommand("--skipApiVersionCheck", "--disableProductStyleUrl")
        .Build();

    private readonly MsSqlContainer sql = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest@sha256:2dca9ee5cd5316952d9b6ef4a0c088ac95b55e3502accdda0fc12ad6ede7b905")
        .Build();

    public string SqlConnectionString { get; private set; } = string.Empty;
    public string BlobConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var root = FindRepositoryRoot();
        await Task.WhenAll(sql.StartAsync(), blob.StartAsync());

        // The Blob SDK recognizes IP literals and localhost as path-style Azurite endpoints.
        // Testcontainers can advertise a DNS name such as host.docker.internal instead.
        // DbConnectionStringBuilder would quote AccountKey (it ends with '='), which the Storage SDK cannot
        // parse, so only the BlobEndpoint segment is replaced in the original connection string.
        var parts = blob.GetConnectionString().Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var index = parts.FindIndex(p => p.StartsWith("BlobEndpoint=", StringComparison.OrdinalIgnoreCase));
        var endpoint = new UriBuilder(parts[index]["BlobEndpoint=".Length..]);
        if (!endpoint.Uri.IsLoopback && endpoint.Uri.HostNameType == UriHostNameType.Dns)
        {
            var addresses = await Dns.GetHostAddressesAsync(endpoint.Host, AddressFamily.InterNetwork);
            endpoint.Host = addresses[0].ToString();
            parts[index] = "BlobEndpoint=" + endpoint.Uri.AbsoluteUri;
        }
        BlobConnectionString = string.Join(';', parts);

        await using (var connection = new SqlConnection(sql.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE DATABASE BatchIntegrationTests";
            await command.ExecuteNonQueryAsync();
        }

        SqlConnectionString = new SqlConnectionStringBuilder(sql.GetConnectionString())
        {
            InitialCatalog = "BatchIntegrationTests"
        }.ConnectionString;

        var schema = Path.Combine(root, "src", "db");
        await using var database = new SqlConnection(SqlConnectionString);
        await database.OpenAsync();
        await using var ddl = database.CreateCommand();
        ddl.CommandText = await File.ReadAllTextAsync(Path.Combine(schema, "Functions", "dbo.fnToHiragana.sql"));
        await ddl.ExecuteNonQueryAsync();
        foreach (var table in new[] { "Publishers", "Authors", "Series", "SeriesAuthors", "Volumes", "ThumbnailAssets", "BatchRuns", "FailedItems" })
        {
            ddl.CommandText = await File.ReadAllTextAsync(Path.Combine(schema, "Tables", $"dbo.{table}.sql"));
            await ddl.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await blob.DisposeAsync();
        await sql.DisposeAsync();
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "db", "ComiCal.Database.sqlproj")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the database schema.");
    }
}
