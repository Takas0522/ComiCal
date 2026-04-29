# 12. バックエンド API 仕様 (Azure Functions)

## 12.1 ランタイム / スタイル

- **.NET 10 Isolated Worker**（In-Process は使わない）。
- スタイル: **REST + OpenAPI**（Swashbuckle）。仕様書 `docs/api/openapi.yaml` を SoT として `update-openapi` Skill で同期。
- バージョニング: **URL パスバージョニング** (`/api/v1/...`)。Major のみバージョン化。
- レイヤリング: **Clean Architecture**。
  - `ComiCal.Api`（HTTP Trigger / Middleware / DTO）
  - `ComiCal.Application`（UseCase / Validator / Mapping）
  - `ComiCal.Domain`（Entity / VO / DomainService / RepositoryInterface）
  - `ComiCal.Infrastructure.*`（EF Core / Blob / Rakuten / KV / AppConfig）
  - `ComiCal.Shared`（DTO / Result / Errors）

## 12.2 主要エンドポイント（v1）

| Method | Path | 説明 | 認証 |
|---|---|---|---|
| GET | `/api/v1/series` | シリーズ検索（q, releaseFrom, publisher, page）| optional |
| GET | `/api/v1/series/{id}` | シリーズ詳細 + 巻 | optional |
| GET | `/api/v1/volumes/upcoming` | 直近発売予定（keyset cursor）| optional |
| GET | `/api/v1/volumes/calendar` | カレンダービュー（year, month or week）| optional |
| GET | `/api/v1/me/subscriptions` | 自分の購読一覧 | required |
| POST | `/api/v1/me/subscriptions` | 購読登録 | required |
| DELETE | `/api/v1/me/subscriptions/{seriesId}` | 購読解除（論理削除）| required |
| PUT | `/api/v1/me/purchases/{volumeId}` | 購入状態更新（State）| required |
| POST | `/api/v1/me/sync/qr` | 匿名データ QR 同期用 Blob 発行 | optional |
| GET | `/api/v1/me/sync/qr/{token}` | QR 同期データ取得 | optional |
| POST | `/api/v1/me/account/delete` | アカウントソフト削除 | required |
| GET | `/api/v1/admin/batch-runs` | バッチ履歴 | Admin |
| POST | `/api/v1/admin/batch/trigger` | バッチ手動起動（Batch Function App へ委譲）| Admin |

## 12.3 リクエスト / レスポンス

- リクエスト DTO: `record` 型 + DataAnnotations + **FluentValidation** で詳細検証。
- レスポンス: `record` の DTO を返す。Domain Entity を直接返さない。
- ページネーション: keyset 形式 `{ items, nextCursor }`。OFFSET は使わない。

## 12.4 エラーレスポンス（RFC 7807）

```json
{
  "type": "https://comical.example.jp/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "...",
  "traceId": "00-abcdef-...",
  "errors": { "title": ["required"] }
}
```

- すべてのエラーを `ProblemDetailsMiddleware` で正規化。
- `traceId` は W3C Trace Context 由来。

## 12.5 ミドルウェア構成（`Program.cs`）

```
HostBuilder
  .ConfigureFunctionsWebApplication()
  .UseMiddleware<CorrelationMiddleware>()      // traceId 付与
  .UseMiddleware<SwaAuthMiddleware>()          // x-ms-client-principal 検証
  .UseMiddleware<UserResolutionMiddleware>()   // IdentityLinks → UserId
  .UseMiddleware<RateLimitMiddleware>()        // 簡易 IP/UserId rate limit
  .UseMiddleware<ProblemDetailsMiddleware>()   // 例外 → RFC 7807
```

## 12.6 認可

- すべての関数属性は **`AuthorizationLevel.Function`**（SWA Linked 前提）。
- Admin 操作は `[RequireRole("Admin")]` カスタム属性 + `UserResolutionMiddleware` で `Users.Role` を確認。

## 12.7 バリデーション

- **FluentValidation** をユースケース単位で実装（`Application/Validators/`）。
- API レイヤーでは入力 DTO の表面検証（必須 / 長さ）のみ。ドメイン規約は Application 層で。

## 12.8 ロギング / テレメトリ

- `ILogger<T>` 構造化ログ。ログテンプレート文字列の補間禁止（`logger.LogInformation("got {Count} items", count)`）。
- Application Insights へ送信。ペイロードは PII を含めない（IdP Subject の hash 表記のみ）。
- `OperationId` を Activity 経由で伝播し、Functions API ↔ Batch ↔ DB 間で串刺し可能にする。

## 12.9 レートリミット

- API Management は **導入しない**。
- Functions ミドルウェアで以下を実装:
  - 認証済み: UserId ごとに Sliding Window 60 req/min。
  - 匿名: IP ごとに Sliding Window 30 req/min。
  - 上限超過時は `429 Too Many Requests` + `Retry-After` ヘッダ。

## 12.10 キャッシュ

- API レスポンスに **`ETag`** を付与し、`If-None-Match` で `304` を返す。
- 主に `/api/v1/volumes/upcoming` と `/api/v1/volumes/calendar` を対象。
- ETag は `(LastBatchRunCompletedAt, queryHash)` から計算。

## 12.11 OpenAPI

- Swashbuckle で生成。CI で `redocly lint` を必須化。
- フロント TS クライアントは `openapi.yaml` から **自動生成**（`update-openapi` Skill）。

## 12.12 CancellationToken

- すべての Function / UseCase / Repository は `CancellationToken` を受け取り、I/O API に伝搬する。
