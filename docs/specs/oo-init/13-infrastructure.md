# 13. インフラ仕様 (Azure / Bicep)

## 13.1 IaC ポリシー

- **Bicep** で Azure リソースを宣言的に管理。手動変更禁止（CI で `what-if` ドリフト検出）。
- ファイル構成:
  ```
  infra/
    main.bicep
    modules/
      network.bicep        # 仮想ネットワークを使う場合のみ。MVP は省略可
      data.bicep           # SQL / Storage
      app.bicep            # SWA / Functions / KV / AppConfig
      observability.bicep  # App Insights / Log Analytics / Alerts
    params/
      dev.bicepparam
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
| Function App (Batch) | `cmcl-prod-jpe-func-batch` |
| SQL Server | `cmcl-prod-jpe-sql` |
| SQL Database | `cmcl-prod-jpe-sqldb` |
| Storage Account | `cmclprodjpest`（- 不可、24 文字制限）|
| Key Vault | `cmcl-prod-jpe-kv` |
| App Configuration | `cmcl-prod-jpe-appcfg` |
| Application Insights | `cmcl-prod-jpe-appi` |
| Log Analytics | `cmcl-prod-jpe-log` |

## 13.3 リソース一覧

| カテゴリ | リソース | 備考 |
|---|---|---|
| Hosting | Static Web Apps Standard | Managed Functions で SSR 実行 |
| Compute | Function App (Consumption) ×2 | API / Batch を分離 |
| Data | Azure SQL Database Serverless | GP_S_Gen5 1vCore, auto-pause 60min |
| Data | Storage Account (StorageV2) | Blob (covers, sync-tmp), Queue (DLQ) |
| Identity | Managed Identity (System assigned) | 各 Function App / SWA |
| Secrets | Key Vault | RBAC モード, soft-delete + purge protection |
| Config | App Configuration Standard | Feature Flag 用 |
| Observability | Application Insights (Workspace-based) | Workspace = Log Analytics |
| Observability | Log Analytics Workspace | 30 日保持 (dev) / 90 日 (prod) |
| Observability | Action Group + Alert Rules | Slack/Teams Webhook |

## 13.4 Bicep モジュール内ルール

- すべての `param` / `output` に **`@description()`** を付与必須。
- シークレット系の param は **`@secure()`**。
- API バージョンは **latest stable**。
- **Managed Identity を必須**。Functions / SWA は System Assigned で発行し、KV へは RBAC ロール `Key Vault Secrets User` を割当。
- App Settings に **Key Vault 参照** (`@Microsoft.KeyVault(SecretUri=...)`) でシークレットを注入。

## 13.5 環境差分（params）

| Param | dev | prod |
|---|---|---|
| `sql.tier` | `GP_S_Gen5_1` | `GP_S_Gen5_2` |
| `sql.autoPauseDelay` | 60 | 60 |
| `log.retentionDays` | 30 | 90 |
| `swa.sku` | Standard | Standard |
| `appConfig.featureFlags.discovery_recommend` | false | false |

## 13.6 ネットワーク

- MVP では **VNet 統合を行わない**（コスト最小）。
- Functions → SQL は SQL の **Allow Azure Services** + Managed Identity 認証で接続。
- 将来 Private Endpoint 化を見越し、Bicep に switch param `network.privateEndpointEnabled` を予約。

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
- 順序: `db` (sqlpackage publish) → `infra` (bicep deploy) → `backend` (func publish) → `frontend` (swa deploy)。
- ロールバック: Bicep の前バージョンを再デプロイ + DB は失敗時自動で前 publish profile を流す。

## 13.11 コスト最小化

- Auto-pause 必須。
- Function は **Consumption Plan**（Premium / Dedicated 不可）。
- Log Analytics 保持は最短。
- Storage は LRS / Cool tier 検討（covers は Hot）。
