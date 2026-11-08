using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Infrastructure.Sql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace ComiCal.Infrastructure.Tests.Sql;

public sealed class PrefixSearchFixture : IAsyncLifetime
{
    private IFutureDockerImage? image;
    private MsSqlContainer? container;
    private string connectionString = string.Empty;

    public Guid SeriesId { get; private set; }
    public Guid VolumeId { get; private set; }
    public DateTime ReleaseDate { get; } = DateTime.UtcNow.Date.AddDays(10);

    public ComiCalDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ComiCalDbContext>()
            .UseSqlServer(connectionString)
            .Options);

    public async Task InitializeAsync()
    {
        var root = FindRepositoryRoot();
        image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(Path.Combine(root, ".devcontainer", "sqlserver"))
            .WithDockerfile("Dockerfile")
            .Build();
        await image.CreateAsync();
        container = new MsSqlBuilder(image.FullName)
            .Build();
        await container.StartAsync();

        await using (var connection = new SqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE DATABASE PrefixSearchTests";
            await command.ExecuteNonQueryAsync();
        }

        connectionString = new SqlConnectionStringBuilder(container.GetConnectionString())
        {
            InitialCatalog = "PrefixSearchTests"
        }.ConnectionString;

        await using var db = CreateContext();
        var functionSql = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "db", "Functions", "dbo.fnToHiragana.sql"));
        await db.Database.ExecuteSqlRawAsync(functionSql);
        foreach (var table in new[] { "Publishers", "Authors", "Series", "SeriesAuthors", "Volumes", "ThumbnailAssets" })
        {
            var tableSql = await File.ReadAllTextAsync(
                Path.Combine(root, "src", "db", "Tables", $"dbo.{table}.sql"));
            await db.Database.ExecuteSqlRawAsync(tableSql);
        }

        var author = Author.Create("木々津克久", TitleNormalizer.Normalize("木々津克久"));
        db.Authors.Add(author);
        var series = CreateSeries("フランケン・ふらん Frantic 13", author.AuthorId);
        SeriesId = series.SeriesId;
        var volume = Volume.Create(series.SeriesId, "9784253019644", 13, ReleaseDate);
        VolumeId = volume.VolumeId;
        db.Series.Add(series);
        db.Volumes.Add(volume);

        var deletedSeries = CreateSeries("フランケン・ふらん Frantic 14", author.AuthorId);
        deletedSeries.SoftDelete();
        db.Series.Add(deletedSeries);
        db.Volumes.Add(Volume.Create(deletedSeries.SeriesId, "9784253019645", 14, ReleaseDate));
        var deletedVolume = Volume.Create(series.SeriesId, "9784253019646", 15, ReleaseDate);
        deletedVolume.SoftDelete();
        db.Volumes.Add(deletedVolume);

        var otherSeries = CreateSeries("フランス旅行 1", author.AuthorId);
        db.Series.Add(otherSeries);
        db.Volumes.Add(Volume.Create(otherSeries.SeriesId, "9784253019647", 1, ReleaseDate));
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlRawAsync("""
            CREATE FULLTEXT CATALOG [FTCatalog] WITH ACCENT_SENSITIVITY = ON;
            CREATE FULLTEXT INDEX ON [dbo].[Series]
                ([NormalizedTitleHiragana] LANGUAGE 1041)
                KEY INDEX [PK_Series] ON [FTCatalog]
                WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM;
            """);
        var fullTextSql = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "db", "Scripts", "PostDeploy", "03-fulltext-indexes.sql"));
        await db.Database.ExecuteSqlRawAsync(fullTextSql);
        await db.Database.ExecuteSqlRawAsync(fullTextSql);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        while (await db.Database.SqlQuery<int>($"""
            SELECT CASE WHEN
                OBJECTPROPERTYEX(OBJECT_ID(N'dbo.Authors'), 'TableFulltextItemCount') >= 1
                AND (SELECT COUNT(*) FROM [dbo].[Series]
                     WHERE CONTAINS([NormalizedTitlePrefixHiragana], '"frantic*"', LANGUAGE 0)) >= 2
                THEN 1 ELSE 0 END AS [Value]
            """).SingleAsync(timeout.Token) == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
        }
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
            await container.DisposeAsync();
        if (image is not null)
            await image.DeleteAsync();
    }

    private static Series CreateSeries(string title, Guid authorId)
    {
        var series = Series.Create(title, SeriesAggregator.ComputeNormalizedTitle(title));
        series.SetPrimaryAuthor(authorId);
        series.AddSeriesAuthor(SeriesAuthor.Create(series.SeriesId, authorId, SeriesAuthorRole.Primary));
        return series;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "db", "ComiCal.Database.sqlproj")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the database schema.");
    }
}
