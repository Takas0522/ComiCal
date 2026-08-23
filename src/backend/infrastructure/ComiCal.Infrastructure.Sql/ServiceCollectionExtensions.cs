using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Sql.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Infrastructure.Sql;

/// <summary>
/// Azure SQL Serverless の auto-pause/auto-resume に対する接続・リトライ挙動を
/// 呼び出し元（API / Batch）ごとに変えるためのオプション。
/// </summary>
/// <param name="ConnectTimeoutSeconds">
/// SqlConnectionStringBuilder.ConnectTimeout に設定する秒数。
/// </param>
/// <param name="MaxRetryCount">EnableRetryOnFailure の maxRetryCount。</param>
/// <param name="MaxRetryDelay">EnableRetryOnFailure の maxRetryDelay。</param>
/// <param name="CommandTimeoutSeconds">EF Core の CommandTimeout（秒）。</param>
public sealed record SqlInfrastructureOptions(
    int ConnectTimeoutSeconds,
    int MaxRetryCount,
    TimeSpan MaxRetryDelay,
    int CommandTimeoutSeconds)
{
    /// <summary>
    /// API (Functions HTTP) 向けの既定値。
    /// ユーザーがリクエスト中に長時間待たされないよう、フェイルファストに倒す。
    /// ConnectTimeout=10s + maxRetryCount=1 (delay 2s) で、サーバー内滞留は最大 15 秒程度に収め、
    /// 40613 等の transient エラーは 503 Service Unavailable + Retry-After でフロントに委譲する
    /// （フロント側の retry.interceptor が Retry-After を尊重して再試行する）。
    /// </summary>
    public static SqlInfrastructureOptions ApiDefaults { get; } = new(
        ConnectTimeoutSeconds: 10,
        MaxRetryCount: 1,
        MaxRetryDelay: TimeSpan.FromSeconds(2),
        CommandTimeoutSeconds: 30);

    /// <summary>
    /// Batch (Durable Functions) 向けの既定値。
    /// ユーザーに直接見えない非同期処理のため、Azure SQL Serverless の
    /// auto-resume（60〜120 秒程度）を確実に吸収できるよう長めに待つ。
    /// Opt2 (WarmupBatchTimer による事前 resume) が効いていれば通常はここまで待たないが、
    /// warm-up が外れた場合のフォールバックとして維持する。
    /// </summary>
    public static SqlInfrastructureOptions BatchDefaults { get; } = new(
        ConnectTimeoutSeconds: 90,
        MaxRetryCount: 8,
        MaxRetryDelay: TimeSpan.FromSeconds(30),
        CommandTimeoutSeconds: 30);
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Batch 向けの既定オプション（<see cref="SqlInfrastructureOptions.BatchDefaults"/>）で登録する
    /// 後方互換オーバーロード。既存の呼び出し元（Batch の Program.cs）の挙動を変えない。
    /// </summary>
    public static IServiceCollection AddSqlInfrastructure(
        this IServiceCollection services, string connectionString)
        => services.AddSqlInfrastructure(connectionString, SqlInfrastructureOptions.BatchDefaults);

    public static IServiceCollection AddSqlInfrastructure(
        this IServiceCollection services, string connectionString, SqlInfrastructureOptions options)
    {
        // Azure SQL Serverless の自動一時停止からの復旧に備え ConnectTimeout を設定する。
        // API は短め（フェイルファスト）、Batch は長め（吸収優先）に呼び出し元で使い分ける。
        var csb = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = options.ConnectTimeoutSeconds,
        };

        services.AddDbContext<ComiCalDbContext>(opt =>
            opt.UseSqlServer(csb.ConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: options.MaxRetryCount,
                    maxRetryDelay: options.MaxRetryDelay,
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
            }));

        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IVolumeRepository, VolumeRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IPublisherRepository, PublisherRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IThumbnailAssetRepository, ThumbnailAssetRepository>();
        services.AddScoped<IBatchRunRepository, BatchRunRepository>();

        return services;
    }
}
