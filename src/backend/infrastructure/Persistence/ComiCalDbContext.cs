using System.Reflection;
using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Persistence;

/// <summary>
/// Primary EF Core DbContext for ComiCal. Mappings live in
/// <see cref="ComiCal.Infrastructure.Persistence.Configurations"/> and are
/// applied by convention via reflection so each aggregate stays self-contained.
/// </summary>
public class ComiCalDbContext : DbContext
{
    public ComiCalDbContext(DbContextOptions<ComiCalDbContext> options)
        : base(options)
    {
    }

    public DbSet<Volume> Volumes => Set<Volume>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<ThumbnailAsset> ThumbnailAssets => Set<ThumbnailAsset>();
    public DbSet<SeriesAuthor> SeriesAuthors => Set<SeriesAuthor>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<SyncToken> SyncTokens => Set<SyncToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
