---
description: 'Use when implementing EF Core 10 (Database First / DACPAC scaffold), Rakuten API client with RateLimiter or Polly, Azure Blob Storage, Key Vault, or App Configuration integrations under src/backend/infrastructure/.'
applyTo: 'src/backend/infrastructure/**'
---

# Backend Infrastructure Layer Instructions

## レイヤーの責務

- 配置: `src/backend/infrastructure/`
  - `ComiCal.Infrastructure/` — 共通: KV / AppConfig / Logging
  - `ComiCal.Infrastructure.Sql/` — EF Core 10（DB First scaffold）
  - `ComiCal.Infrastructure.Blob/` — Azure Blob Storage
  - `ComiCal.Infrastructure.Rakuten/` — 楽天 Books API クライアント + RateLimiter

- **EF Core / Azure SDK / 外部 API クライアントはここのみ** に存在
- Domain / Application で定義されたインターフェイスを実装

## EF Core 10（Database First）

- **SSDT/DACPAC が SoT**：EF Core はそこから scaffold する
- scaffold コマンド:
  ```bash
  dotnet ef dbcontext scaffold "Server=...;Database=...;..." \
    Microsoft.EntityFrameworkCore.SqlServer \
    --output-dir Models --force
  ```
- scaffold 後の手動拡張は **`partial class`** で別ファイルに（`*.Custom.cs`）。直接編集すると次回 scaffold で消える

## クエリ規約

- **必要なカラムのみ射影**：`Select(x => new { x.Id, x.Name })`
- **トラッキング不要なら `AsNoTracking()`**
- **適切なページネーション**: keyset pagination（OFFSET ではなく `Where(x => x.Id > lastId)`）
- **生 SQL は必ずパラメタライズ**: `FromSqlRaw("EXEC ... {0}", param)` または `Database.SqlQuery<T>($"...")`
- **検索はフルテキスト検索 + 計算列**（ひらがな正規化キー）。`LIKE` 使用禁止

## EF Core 10 Breaking Changes 注意

- JSON 型のデフォルト動作変更あり（要確認）
- Stored Procedure マッピング API 改善
- 詳細: [EF Core 10 Breaking Changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes)

## 楽天 Books API クライアント

- 配置: `ComiCal.Infrastructure.Rakuten/`
- **RateLimiter（1 req/sec）必須**：`System.Threading.RateLimiting.TokenBucketRateLimiter` または Polly の `RateLimitPolicy`
- リトライポリシー（Polly）：5xx と HttpRequestException で指数バックオフ
- `applicationId` は `IOptions<RakutenOptions>` 経由で注入、Key Vault 参照を App Settings に

```csharp
services.AddHttpClient<IRakutenBooksClient, RakutenBooksClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetRateLimitPolicy());
```

## Blob Storage

- Managed Identity で接続（接続文字列ではなく）
- 表紙画像は **Blob から直接配信**（CDN なし）
- アップロード時は `Content-Type` と `Cache-Control` を明示

## セキュリティ

- **EF Core によるパラメタライズドクエリで SQL Injection 対策**
- 生クエリは最小限、必須の場合は必ずパラメータ
- Connection String / API キーはコードに書かない（Key Vault 経由）

## アンチパターン

- ❌ scaffold 結果を直接編集して保持（`partial class` で拡張）
- ❌ パラメタライズされていない `FromSqlRaw($"... {userInput}")`
- ❌ `LIKE` での検索（フルテキスト検索 + 計算列を使う）
- ❌ N+1 クエリ（`Include` または `Select` で射影）
- ❌ 接続文字列のハードコード
- ❌ 楽天 API 呼び出しに RateLimiter なし
