using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.ValueObjects;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases;

/// <summary>ISBN-13 指定で巻 1 件を取得するクエリ。</summary>
public sealed record GetVolumeByIsbnQuery(string Isbn);

/// <summary>ISBN-13 指定で巻 1 件を取得するユースケース。</summary>
public interface IGetVolumeByIsbnUseCase
{
    Task<Result<VolumeDto>> ExecuteAsync(
        GetVolumeByIsbnQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IGetVolumeByIsbnUseCase" />
public sealed class GetVolumeByIsbnUseCase(IVolumeRepository repository) : IGetVolumeByIsbnUseCase
{
    private readonly IVolumeRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<VolumeDto>> ExecuteAsync(
        GetVolumeByIsbnQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        if (!Isbn13.TryCreate(query.Isbn, out var isbn, out var error))
        {
            return Result<VolumeDto>.Failure(ApplicationErrors.InvalidIsbn(error ?? "Invalid ISBN-13."));
        }

        var volume = await _repository.GetByIsbnAsync(isbn!, cancellationToken).ConfigureAwait(false);
        if (volume is null)
        {
            return Result<VolumeDto>.Failure(ApplicationErrors.VolumeNotFound(isbn!.Value));
        }

        return Result<VolumeDto>.Success(volume.ToDto());
    }
}
