---
description: 'Use when writing xUnit v3 backend tests with Testcontainers (MSSQL/Azurite), WebApplicationFactory integration tests, IClassFixture/IAsyncLifetime fixtures, or coverage gates under src/tests/backend/.'
applyTo: 'src/tests/backend/**'
---

# Backend Test (xUnit + Testcontainers) Instructions

## フレームワーク

- **xUnit v3**（v2 はメンテナンスモード）
- **Microsoft Testing Platform (MTP)** サポート
- アサーション: `xunit.assert` 標準（FluentAssertions も併用可）

## テスト分類とプロジェクト

| プロジェクト | 範囲 | スタイル |
|------------|------|---------|
| `ComiCal.Domain.Tests` | Domain 層 純粋ロジック | 単体（モックなし） |
| `ComiCal.Application.Tests` | Application 層 UseCase | モック使用 |
| `ComiCal.Api.Tests` | API 統合 | `WebApplicationFactory` |
| `ComiCal.Batch.Tests` | Durable orchestration | Testcontainers (MSSQL/Azurite) |
| `ComiCal.Infrastructure.Tests` | EF Core / 外部 API | Testcontainers |

## 命名規約

- メソッド名: `MethodName_StateUnderTest_ExpectedBehavior`
  - 例: `AddSubscription_WhenAlreadySubscribed_ThrowsConflictException`
- クラス名: `<対象クラス名>Tests`

## Fact / Theory

- 入力なし: `[Fact]`
- パラメタライズド: `[Theory]` + `[InlineData]` / `[MemberData]` / `[ClassData]`

## Fixture

- **共有セットアップ**: `IClassFixture<T>` または `ICollectionFixture<T>`
- DB / コンテナのライフサイクルは Fixture で管理

## Testcontainers パターン

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        // Apply DACPAC, seed data
    }

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}
```

- **公式イメージのバージョン固定**:
  - MSSQL: `mcr.microsoft.com/mssql/server:2022-latest`
  - Azurite: `mcr.microsoft.com/azure-storage/azurite`
- **`IAsyncLifetime`** で非同期ライフサイクル管理
- テスト間の状態リセット: TRUNCATE / Blob 削除

## カバレッジ

- **ラインカバレッジ ≥ 80%** を CI ゲート
- `coverlet.collector` で測定

## 並列実行

- xUnit はデフォルトで並列実行をサポート
- DB を共有する Test は `[Collection("Database")]` で順次実行

## アンチパターン

- ❌ ネットワーク・ファイル・本番 DB に依存するテスト（Testcontainers を使う）
- ❌ private メソッドの直接テスト
- ❌ `Thread.Sleep` 使用（`await Task.Delay` か polling）
- ❌ テスト間の状態共有（ベース状態を Fixture で再構築）
- ❌ `[Fact]` でパラメタライズしたい場合に複数の `[Fact]` メソッドをコピペ（`[Theory]` を使う）
