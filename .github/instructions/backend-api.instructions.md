---
description: 'Use when implementing or reviewing Azure Functions Isolated Worker HTTP API endpoints, RFC 7807 Problem Details, OpenAPI annotations, FluentValidation, DI/middleware in Program.cs, or SWA-linked authorization under src/backend/api/.'
applyTo: 'src/backend/api/**'
---

# Backend API (Azure Functions Isolated Worker) Instructions

## ランタイム

- **.NET 10 Isolated Worker** モデル必須（In-Process は 2026/11 サポート終了）
- パッケージ:
  - `Microsoft.Azure.Functions.Worker`
  - `Microsoft.Azure.Functions.Worker.Sdk`
  - `Microsoft.Azure.Functions.Worker.Extensions.*`
- `Microsoft.NET.Sdk.Functions`（レガシー）使用禁止

## API 設計

- **REST + OpenAPI 生成（Swashbuckle）**
- **URL パスバージョニング** (`/api/v1/...`)
- **エラー応答は RFC 7807 Problem Details**（`application/problem+json`）
- **`Authorization=function`** で SWA 経由限定アクセス

## ミドルウェア

配置: `src/backend/api/ComiCal.Api/Middlewares/`

- **Auth ミドルウェア**: SWA Easy Auth ヘッダーから内部 UserId(GUID) にマッピング
- **RateLimit ミドルウェア**: Functions 内ミドルウェアで実装（API Management 不使用）
- **ProblemDetails ミドルウェア**: 例外を RFC 7807 に変換

`Program.cs` で登録:
```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<AuthMiddleware>();
        builder.UseMiddleware<RateLimitMiddleware>();
        builder.UseMiddleware<ProblemDetailsMiddleware>();
    })
    .Build();
```

## DI

- `Microsoft.Extensions.DependencyInjection` を使用
- `Program.cs` ですべての依存を一元登録
- スコープライフタイム: `AddScoped` を基本、ステートレスサービスは `AddSingleton`

## ロギング

- **`ILogger<T>` 標準** → Application Insights
- 構造化ログ（テンプレート + パラメータ）：`logger.LogInformation("User {UserId} subscribed to {SeriesId}", userId, seriesId);`
- 文字列補間 (`$"..."`) ロギング禁止

## DTO

- 配置: `src/backend/api/ComiCal.Api/Models/`
- **Record 型**で不変に
- 命名: `XxxRequest` / `XxxResponse`
- Domain Entity を直接シリアライズしない

## セキュリティ

- SWA Auth → Easy Auth でヘッダー付与 → Backend で署名 / クレーム検証
- シークレットは Key Vault 参照経由で App Settings に注入
- Managed Identity で Key Vault / App Configuration / Storage にアクセス

## アンチパターン

- ❌ In-Process モデル
- ❌ `Microsoft.NET.Sdk.Functions` への参照
- ❌ Domain Entity を API レスポンスに直接返却
- ❌ 例外メッセージを Problem Details の `detail` にそのまま出す（情報漏洩リスク）
- ❌ 同期 I/O
