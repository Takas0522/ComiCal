# ComiCal — Bicep IaC

Azure インフラストラクチャを Bicep で管理します。環境は **prod のみ** です（Stage/dev は 2026-11 にコスト削減のため廃止。[ADR 0004](../docs/adr/0004-azure-cost-reduction.md)）。
日次バッチは Azure にデプロイせず、メンテナー PC で実行します（`tools/run-batch-local.ps1`）。

## ディレクトリ構成

```
infra/
├── main.bicep                  # サブスクリプションスコープのエントリポイント・RG作成
├── modules/
│   ├── observability.bicep     # Log Analytics / App Insights / Action Group
│   ├── data.bicep              # SQL Database Serverless（無料オファー）/ Storage Account
│   ├── app.bicep               # SWA / Function App (API) / Key Vault
│   ├── batch-operator.bicep    # ローカル Batch 実行者の RBAC（Blob 書き込み / KV シークレット読み取り）
│   └── budget.bicep            # RG の月次予算アラート
└── params/
    └── prod.bicepparam
```

## 前提条件

- Azure CLI (`az`) がインストール済みであること（Bicep は `az bicep` で自動導入）
- OIDC フェデレーテッドクレデンシャルでサブスクリプションへのアクセス権があること
- GitHub Actions（environment: prod）のシークレット / 変数が設定済みであること

## 環境変数（CI/CD）

| 変数名 | 種別 | 説明 |
|---|---|---|
| `SQL_ADMIN_PASSWORD` | secret | SQL Server 管理者パスワード |
| `RAKUTEN_APPLICATION_ID` / `RAKUTEN_ACCESS_KEY` / `RAKUTEN_AFFILIATE_ID` | secret | 楽天 API 認証情報（Key Vault に格納し、ローカル Batch が参照） |
| `ALERT_EMAIL_ADDRESS` | secret/var | アラート・予算通知先（省略可） |
| `BATCH_OPERATOR_PRINCIPAL_ID` | var | ローカル Batch を実行するユーザーの Entra objectId（省略可） |
| `BATCH_OPERATOR_IP_ADDRESS` | var | ローカル Batch を実行するメンテナーの自宅のグローバル IPv4 アドレス。SQL Server にこの IP だけを許可する常設規則 `batch-operator` を作成する。未設定または空の場合は規則の作成をスキップする。 |

## What-If / デプロイ

本番デプロイは `v*` タグの push（`.github/workflows/cd-prod.yml`）で行います。手動の差分確認:

```bash
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/prod.bicepparam
```

## モジュール詳細

### observability.bicep

| リソース | 命名 | 備考 |
|---|---|---|
| Log Analytics Workspace | `cmcl-prod-jpe-log` | 保持 30 日 / 日次上限 0.2 GB |
| Application Insights | `cmcl-prod-jpe-appi` | Workspace-based |
| Action Group | `cmcl-prod-jpe-ag` | メール / Webhook（予算通知にも使用） |

ログアラートは常時課金のため配置しません（旧 Batch 失敗アラートは Batch のローカル化に伴い削除）。

### data.bicep

| リソース | 命名 | 備考 |
|---|---|---|
| SQL Server | `cmcl-prod-jpe-sql` | System Assigned MI |
| SQL Database | `cmcl-prod-jpe-sqldb` | GP_S_Gen5_2, **無料オファー**（超過時は課金継続）, min 0.5 vCore, auto-pause 15 分, 32 GB |
| Storage Account | `cmclprodjpest` | StorageV2 / Standard_LRS |
| Blob Container | `covers` | public read (表紙画像直接配信) |
| Blob Container | `sync-tmp` | private（1 日で自動削除） |
| Blob Container | `app-package-api` | Flex Consumption のデプロイパッケージ |
| Queue | `failed-items-dlq` | バッチ失敗キュー |

> 既存の有償 DB は無料オファーに変換できません。`sqlUseFreeLimit = true` の状態でデプロイする前に
> `plan/scripts/apply-07-sql-free-offer.ps1` で DB を移行してください。

### app.bicep

| リソース | 命名 | 備考 |
|---|---|---|
| Key Vault | `cmcl-prod-jpe-kv` | RBAC モード, Soft-delete, Purge protection |
| Static Web App | `cmcl-prod-jpe-swa` | Standard（linked backend とカスタム認証のため） |
| Function App (API) | `cmcl-prod-jpe-func-api` | Flex Consumption, .NET 10 Isolated, 最大 10 インスタンス, VNet 統合なし |

API は楽天 API を呼びません（静的な送信元 IP を持たないため。検索は DB のみ）。

**Key Vault シークレット**: `AzureWebJobsStorage`, `AppInsightsConnectionString`, `RakutenAppId`, `RakutenAccessKey`, `RakutenAffiliateId`

### batch-operator.bicep / budget.bicep

- `batchOperatorPrincipalId` 指定時のみ: 実行ユーザーに Storage Blob Data Contributor と Key Vault Secrets User を付与。DB ユーザーは `cd-prod.yml` の Grant ステップで作成。
- `cmcl-prod-jpe-budget`: 月 3,000 円。実績 80% / 100%、予測 100% で Action Group に通知。

## 注意: Incremental デプロイ

`az deployment sub create` は Incremental モードのため、Bicep から削除したリソースは Azure から自動では消えません。
削除は `plan/scripts/apply-0*.ps1` のように明示的に行います。

## ロールバック

```bash
az deployment sub list --query "[?starts_with(name,'deploy-prod')].{name:name,time:properties.timestamp}" -o table
git checkout <previous-sha> -- infra/
az deployment sub create ...
```
