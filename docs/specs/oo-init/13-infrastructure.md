# 13. インフラ仕様 (Azure / Bicep)

> **2026-11 改訂（[ADR 0004](../../adr/0004-azure-cost-reduction.md)）**: Azure を単一環境に整理し、Batch を Windows PC で実行します。
>
> - 環境は prod のみ（Stage/dev 廃止）。`network.bicep` / `dev.bicepparam` / App Configuration / Batch Function App は削除。
> - SQL は無料オファー（min 0.5 vCore / auto-pause 15 分）、月次予算 `cmcl-prod-jpe-budget` を追加。最新の構成は [インフラ README](../../../infra/README.md) を参照。

## 13.1 IaC ポリシー

- **Bicep** で Azure リソースを宣言的に管理。手動変更禁止（CI で `what-if` ドリフト検出）。
- ファイル構成:
  ```
  infra/
    main.bicep
    modules/
      data.bicep           # SQL / Storage
      app.bicep            # SWA / API Functions / Key Vault
      observability.bicep  # App Insights / Log Analytics
      batch-operator.bicep # Batch 実行ユーザーの RBAC
      budget.bicep         # 予算
    params/
      prod.bicepparam
  ```
- **Azure Verified Modules (AVM)** を優先採用。独自モジュールはやむを得ない場合のみ。

## 13.2 命名規則（CAF）

`{prefix}-{env}-{region}-{resource}` 形式:

| Resource | 例 |
|---|---|
| Resource Group | `cmcl-prod-jpe-rg` |
| Static Web App | `cmcl-prod-jpe-swa` |
| Function App (API) | `cmcl-prod-jpe-func-api` |
| SQL Server | `cmcl-prod-jpe-sql` |
| SQL Database | `cmcl-prod-jpe-sqldb` |
| Storage Account | `cmclprodjpest`（- 不可、24 文字制限）|
| Key Vault | `cmcl-prod-jpe-kv` |
| Application Insights | `cmcl-prod-jpe-appi` |
| Log Analytics | `cmcl-prod-jpe-log` |

## 13.3 リソース一覧

| カテゴリ | リソース | 備考 |
|---|---|---|
| Hosting | Static Web Apps Standard | Managed Functions で SSR 実行 |
| Compute | Function App (Flex Consumption) | API 用 |
| Data | Azure SQL Database Serverless | min 0.5 vCore、auto-pause 15 分 |
| Data | Storage Account (StorageV2) | Blob (covers, sync-tmp) |
| Identity | Managed Identity (System assigned) | 各 Function App / SWA |
| Secrets | Key Vault | RBAC モード, soft-delete + purge protection |
| Observability | Application Insights (Workspace-based) | Workspace = Log Analytics |
| Observability | Log Analytics Workspace | 30 日保持 |
| Observability | Action Group + Alert Rules | Slack/Teams Webhook |

## 13.4 Bicep モジュール内ルール

- すべての `param` / `output` に **`@description()`** を付与必須。
- シークレット系の param は **`@secure()`**。
- API バージョンは **latest stable**。
- **Managed Identity を必須**。Functions / SWA は System Assigned で発行し、KV へは RBAC ロール `Key Vault Secrets User` を割当。
- App Settings に **Key Vault 参照** (`@Microsoft.KeyVault(SecretUri=...)`) でシークレットを注入。

## 13.5 本番環境の設定

設定値は [prod.bicepparam](../../../infra/params/prod.bicepparam) を参照。Batch 実行ユーザーと IP は GitHub の `prod` 環境変数 `BATCH_OPERATOR_PRINCIPAL_ID` / `BATCH_OPERATOR_IP_ADDRESS` で指定する。

## 13.6 ネットワーク

- Windows PC の送信元 IP を楽天の許可 IP と SQL の `batch-operator` 規則に登録する。Batch は同じ IP を `Batch:ExpectedPublicIp` で照合する。
- API → SQL は Allow Azure Services 規則と Managed Identity で接続する。Batch → SQL は登録した IP と実行ユーザーの Entra 認証で接続する。

## 13.7 バックアップ / DR

- Azure SQL の **自動バックアップ標準**（PITR 7 日）に依存。
- DR / Geo-restore は不要（DR 不要原則）。
- Blob は **LRS**（コスト最小）。

## 13.8 配信

- 表紙画像は **Blob から直接配信**。CDN / Front Door は将来検討。
- Blob コンテナ `covers` は public read。`sync-tmp` は private。

## 13.9 SWA 構成

- **`staticwebapp.config.json`** をリポジトリルートに配置:
  - ルーティング（SPA fallback）
  - 認証ジャーニー（Entra External ID）
  - **CSP / HSTS / Permissions-Policy ヘッダ**
- API Function App は SWA に **Linked**（`linkedBackend`）。

## 13.10 デプロイフロー

- GitHub Actions OIDC で各環境にログイン。
- 順序: `db` (sqlpackage publish) → `infra` (bicep deploy) → `backend` (API の zip デプロイ) → `frontend` (swa deploy)。
- 日次 Batch は [Windows PC 上で更新](../../runbooks/batch-operations.md)する。
- ロールバック: Bicep の前バージョンを再デプロイ + DB は失敗時自動で前 publish profile を流す。

## 13.11 コスト最小化

- Auto-pause 必須。
- Function は **Consumption Plan**（Premium / Dedicated 不可）。
- Log Analytics 保持は最短。
- Storage は LRS / Cool tier 検討（covers は Hot）。
