using ComiCal.Application.Dtos;
using ComiCal.Application.Interfaces;
using ComiCal.Application.UseCases.Series;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class SearchSeriesUseCaseTests
{
    private readonly ISeriesRepository _seriesRepo = Substitute.For<ISeriesRepository>();
    private readonly IRakutenBookSearchService _rakutenSearch = Substitute.For<IRakutenBookSearchService>();
    private readonly SearchSeriesUseCase _sut;

    public SearchSeriesUseCaseTests()
    {
        _sut = new SearchSeriesUseCase(
            _seriesRepo,
            _rakutenSearch,
            NullLogger<SearchSeriesUseCase>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WithKeyword_WhenDbResultsAboveThreshold_SkipsRakuten()
    {
        // Arrange: 20 DB 結果を返す → 閾値以上なので楽天を呼ばない
        var series = Enumerable.Range(0, 20)
            .Select(i => Series.Create($"タイトル{i}", $"title{i}", null))
            .ToList();
        _seriesRepo.SearchAsync(Arg.Any<SeriesSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns((series.AsReadOnly(), (string?)null));

        var query = new SeriesSearchQuery("テスト", null, null, null, 20);

        // Act
        var result = await _sut.ExecuteAsync(query, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.Empty(result.Value.RakutenCandidates);
        await _rakutenSearch.DidNotReceive().SearchByKeywordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithKeyword_WhenDbResultsBelowThreshold_CallsRakuten()
    {
        // Arrange: DB 結果 1 件 → 閾値未満なので楽天を呼ぶ
        var series = new List<Series> { Series.Create("既存タイトル", "existingtitle", null) };
        _seriesRepo.SearchAsync(Arg.Any<SeriesSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns((series.AsReadOnly(), (string?)null));

        var rakutenItems = new List<RakutenBookSearchItem>
        {
            new("9784000000001", "新しい漫画 1", "著者A", "出版社A", "2026年01月01日", null, null),
            new("9784000000002", "新しい漫画 2", "著者B", "出版社B", null, null, null),
        };
        _rakutenSearch.SearchByKeywordAsync("テスト", Arg.Any<CancellationToken>())
            .Returns(rakutenItems);

        var query = new SeriesSearchQuery("テスト", null, null, null, 20);

        // Act
        var result = await _sut.ExecuteAsync(query, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.RakutenCandidates.Count);
        Assert.Equal("9784000000001", result.Value.RakutenCandidates[0].Isbn);
    }

    [Fact]
    public async Task ExecuteAsync_WithKeyword_WhenRakutenItemMatchesDb_ExcludesFromCandidates()
    {
        // Arrange: DB に "既存漫画" があり、楽天にも同じ正規化タイトルになる "既存漫画 第1巻" がある → 候補に含めない
        // "既存漫画 第1巻" は StripVolumeNumber で " 第1巻" が取り除かれ "既存漫画" になる
        var series = new List<Series> { Series.Create("既存漫画", "既存漫画", null) };
        _seriesRepo.SearchAsync(Arg.Any<SeriesSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns((series.AsReadOnly(), (string?)null));

        var rakutenItems = new List<RakutenBookSearchItem>
        {
            new("9784000000001", "既存漫画 第1巻", "著者A", "出版社A", null, null, null),
            new("9784000000002", "全然違う漫画", "著者B", "出版社B", null, null, null),
        };
        _rakutenSearch.SearchByKeywordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rakutenItems);

        var query = new SeriesSearchQuery("既存漫画", null, null, null, 20);

        // Act
        var result = await _sut.ExecuteAsync(query, null);

        // Assert
        Assert.True(result.IsSuccess);
        // "既存漫画 第1巻" の正規化タイトルが DB の "既存漫画" と一致するので除外される
        Assert.Single(result.Value.RakutenCandidates);
        Assert.Equal("全然違う漫画", result.Value.RakutenCandidates[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRakutenThrows_ReturnsDbResultsOnly()
    {
        // Arrange: 楽天 API が例外を投げる → DB 結果のみ返す
        var series = new List<Series> { Series.Create("タイトル", "title", null) };
        _seriesRepo.SearchAsync(Arg.Any<SeriesSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns((series.AsReadOnly(), (string?)null));

        _rakutenSearch.SearchByKeywordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RakutenBookSearchItem>>(new HttpRequestException("API error")));

        var query = new SeriesSearchQuery("テスト", null, null, null, 20);

        // Act
        var result = await _sut.ExecuteAsync(query, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Empty(result.Value.RakutenCandidates);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutKeyword_NeverCallsRakuten()
    {
        // Arrange: キーワードなしの場合は楽天を呼ばない
        _seriesRepo.SearchAsync(Arg.Any<SeriesSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Series>().AsReadOnly(), (string?)null));

        var query = new SeriesSearchQuery(null, null, null, null, 20);

        // Act
        var result = await _sut.ExecuteAsync(query, null);

        // Assert
        Assert.True(result.IsSuccess);
        await _rakutenSearch.DidNotReceive().SearchByKeywordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
