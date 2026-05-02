# ComiCal — Bicep IaC

Azure インフラストラクチャを Bicep で管理します。ファイル構成と各モジュールの責務は以下の通りです。

## ディレクトリ構成

```
infra/
├── main.bicep                  # サブスクリプションスコープのエントリポイント・RG作成
├── modules/
│   ├── observability.bicep     # Log Analytics / App Insights / Action Group / Alert
│   ├── data.bicep              # SQL Database Serverless / Storage Account
│   └── app.bicep               # SWA / Function Apps / Key Vault / App Configuration
└── params/
    ├── dev.bicepparam
    └── prod.bicepparam
```

## 前提条件

- Azure CLI (`az`) がインストール済みであること
- Bicep CLI (`bicep`) がインストール済みであること
- OIDC フェデレーテッドクレデンシャルでサブスクリプションへのアクセス権があること
- GitHub Actions シークレット `SQL_ADMIN_PASSWORD` が設定済みであること

## 環境変数

| 変数名 | 説明 |
|---|---|
| `SQL_ADMIN_PASSWORD` | SQL Server 管理者パスワード（CI/CD シークレット） |
| `ALERT_WEBHOOK_URL` | Slack / Teams 通知 URL（省略可） |

## 構文チェック

```bash
bicep build infra/main.bicep
```

## What-If (差分確認)

```bash
# dev 環境
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/dev.bicepparam \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD"

# prod 環境
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/prod.bicepparam \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
  --parameters alertWebhookUrl="$ALERT_WEBHOOK_URL"
```

## デプロイ

```bash
# dev 環境
az deployment sub create \
  --location japaneast \
  --name "cmcl-dev-$(date +%Y%m%d-%H%M%S)" \
  --template-file infra/main.bicep \
  --parameters infra/params/dev.bicepparam \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD"

# prod 環境（手動承認後）
az deployment sub create \
  --location japaneast \
  --name "cmcl-prod-$(date +%Y%m%d-%H%M%S)" \
  --template-file infra/main.bicep \
  --parameters infra/params/prod.bicepparam \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
  --parameters alertWebhookUrl="$ALERT_WEBHOOK_URL"
```

## モジュール詳細

### observability.bicep

| リソース | 命名 | 備考 |
|---|---|---|
| Log Analytics Workspace | `cmcl-{env}-jpe-log` | dev=30日 / prod=90日保持 |
| Application Insights | `cmcl-{env}-jpe-appi` | Workspace-based (LogAnalytics) |
| Action Group | `cmcl-{env}-jpe-ag` | Webhook optional |
| Alert Rule | `cmcl-{env}-jpe-alert-batch-failed` | batch.failedItem >= 5 で発火 |

### data.bicep

| リソース | 命名 | 備考 |
|---|---|---|
| SQL Server | `cmcl-{env}-jpe-sql` | System Assigned MI |
| SQL Database | `cmcl-{env}-jpe-sqldb` | GP_S_Gen5, auto-pause 60分 |
| Storage Account | `cmcl{env}jpest` | StorageV2 / Standard_LRS |
| Blob Container | `covers` | public read (表紙画像直接配信) |
| Blob Container | `sync-tmp` | private (バッチ一時ファイル) |
| Queue | `failed-items-dlq` | バッチ失敗キュー |

> **補足**: `sync-tmp` コンテナのライフサイクルポリシーは 1 日削除（Azure Blob ライフサイクル管理の最小単位）。
> 設計上の 5 分 TTL はアプリケーション側（バッチ完了後に即削除）で担保します。

### app.bicep

| リソース | 命名 | 備考 |
|---|---|---|
| Key Vault | `cmcl-{env}-jpe-kv` | RBAC モード, Soft-delete |
| Static Web App | `cmcl-{env}-jpe-swa` | Standard, Linked Backend |
| Function App (API) | `cmcl-{env}-jpe-func-api` | Consumption Linux, .NET 10 Isolated |
| Function App (Batch) | `cmcl-{env}-jpe-func-batch` | Consumption Linux, .NET 10 Isolated |
| App Configuration | `cmcl-{env}-jpe-appcfg` | Standard, Feature Flags |

**Key Vault シークレット**:
- `AzureWebJobsStorage` — Storage Account 接続文字列
- `AppInsightsConnectionString` — Application Insights 接続文字列

**Feature Flags** (すべて初期値 `false`):
- `discovery-recommend`
- `calendar-ab-test`
- `entra-login-rollout`

## ロールバック

```bash
# 直前のデプロイ名を確認
az deployment sub list --query "[?starts_with(name,'cmcl-dev')].{name:name,time:properties.timestamp}" -o table

# 直前バージョンの Bicep を再デプロイ（Git で前コミットを取得して再実行）
git checkout <previous-sha> -- infra/
az deployment sub create ...
```
