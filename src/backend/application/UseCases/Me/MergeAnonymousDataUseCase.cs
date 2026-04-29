using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases.Me;

/// <summary>匿名→ログイン マージコマンド。</summary>
public sealed record MergeAnonymousDataCommand(
    IReadOnlyList<MergeAnonymousSubscriptionItem> Subscriptions,
    IReadOnlyList<MergeAnonymousPurchaseItem> Purchases);

/// <summary>匿名 IndexedDB に保持されたデータをログインユーザーへ取り込むユースケース。</summary>
/// <remarks>
/// 1. 全アイテムを単一トランザクションで処理（途中失敗時はロールバックして 500）。
/// 2. 各アイテムは既存リポジトリの冪等 UPSERT を呼び出す（同一キーの再実行は安全）。
/// 3. 参照先（Series / Volume）が存在しない ID は失敗ではなく <see cref="MergeResultDto.Skipped"/> に積む。
/// 4. 入力が空でも成功（no-op）。バリデーションは <see cref="FluentValidation"/> でサイズ上限のみ担保。
/// </remarks>
public interface IMergeAnonymousDataUseCase
{
    Task<Result<MergeResultDto>> ExecuteAsync(
        MergeAnonymousDataCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IMergeAnonymousDataUseCase" />
public sealed class MergeAnonymousDataUseCase(
    IValidator<MergeAnonymousDataCommand> validator,
    ISubscriptionRepository subscriptions,
    IPurchaseRepository purchases,
    ISeriesRepository series,
    IVolumeRepository volumes,
    IUnitOfWork unitOfWork) : IMergeAnonymousDataUseCase
{
    private readonly IValidator<MergeAnonymousDataCommand> _validator = validator;
    private readonly ISubscriptionRepository _subscriptions = subscriptions;
    private readonly IPurchaseRepository _purchases = purchases;
    private readonly ISeriesRepository _series = series;
    private readonly IVolumeRepository _volumes = volumes;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <inheritdoc />
    public async Task<Result<MergeResultDto>> ExecuteAsync(
        MergeAnonymousDataCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result<MergeResultDto>.Failure(MeErrors.AuthRequired());
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<MergeResultDto>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        // 空ペイロードは早期成功（DB アクセス不要）。
        if (command.Subscriptions.Count == 0 && command.Purchases.Count == 0)
        {
            return Result<MergeResultDto>.Success(new MergeResultDto(
                new MergeCountDto(0, 0),
                new MergeSkippedDto(Array.Empty<Guid>(), Array.Empty<Guid>())));
        }

        try
        {
            var dto = await _unitOfWork.ExecuteInTransactionAsync(
                ct => MergeAllAsync(userId, command, ct),
                cancellationToken).ConfigureAwait(false);

            return Result<MergeResultDto>.Success(dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<MergeResultDto>.Failure(
                Error.Unexpected("merge-failed", $"Failed to merge anonymous data: {ex.Message}"));
        }
    }

    private async Task<MergeResultDto> MergeAllAsync(
        Guid userId,
        MergeAnonymousDataCommand command,
        CancellationToken cancellationToken)
    {
        var skippedSubs = new List<Guid>();
        var skippedPurchases = new List<Guid>();
        var seenSeries = new HashSet<Guid>();
        var seenVolumes = new HashSet<Guid>();
        var mergedSubs = 0;
        var mergedPurchases = 0;

        foreach (var item in command.Subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!seenSeries.Add(item.SeriesId))
            {
                continue;
            }

            var target = await _series.GetByIdAsync(item.SeriesId, cancellationToken).ConfigureAwait(false);
            if (target is null)
            {
                skippedSubs.Add(item.SeriesId);
                continue;
            }

            await _subscriptions.UpsertAsync(userId, item.SeriesId, cancellationToken).ConfigureAwait(false);
            mergedSubs++;
        }

        foreach (var item in command.Purchases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!seenVolumes.Add(item.VolumeId))
            {
                continue;
            }

            var volume = await _volumes.GetByIdAsync(item.VolumeId, cancellationToken).ConfigureAwait(false);
            if (volume is null)
            {
                skippedPurchases.Add(item.VolumeId);
                continue;
            }

            await _purchases.UpsertAsync(userId, item.VolumeId, item.PurchasedAt, cancellationToken).ConfigureAwait(false);
            mergedPurchases++;
        }

        return new MergeResultDto(
            new MergeCountDto(mergedSubs, mergedPurchases),
            new MergeSkippedDto(skippedSubs, skippedPurchases));
    }
}
