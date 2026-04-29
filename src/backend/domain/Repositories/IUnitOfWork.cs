namespace ComiCal.Domain.Repositories;

/// <summary>
/// 単一トランザクション境界を提供する Unit of Work 抽象。
/// 複数リポジトリ操作を Atomic にまとめたいユースケース（例: 匿名→ログイン マージ）から利用する。
/// </summary>
/// <remarks>
/// Application 層は永続化技術に非依存である必要があるため、トランザクション境界もインターフェース経由で表現する。
/// 実装は Infrastructure 層の <c>UnitOfWork</c>（EF Core <c>BeginTransactionAsync</c>）が担う。
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// <paramref name="action"/> を 1 つのデータベーストランザクション内で実行し、結果を返す。
    /// 例外が投げられた場合はロールバックして再スローする。
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken);
}
