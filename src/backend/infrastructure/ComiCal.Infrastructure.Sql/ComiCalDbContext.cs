using ComiCal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql;

public class ComiCalDbContext(DbContextOptions<ComiCalDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<IdentityLink> IdentityLinks => Set<IdentityLink>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<SeriesAuthor> SeriesAuthors => Set<SeriesAuthor>();
    public DbSet<Volume> Volumes => Set<Volume>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<ThumbnailAsset> ThumbnailAssets => Set<ThumbnailAsset>();
    public DbSet<BatchRun> BatchRuns => Set<BatchRun>();
    public DbSet<FailedItem> FailedItems => Set<FailedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComiCalDbContext).Assembly);
    }
}
