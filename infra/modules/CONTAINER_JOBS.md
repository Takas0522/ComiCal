# Container Jobs Module

このモジュールは、ComiCal アプリケーション用の Azure Container Jobs（スケジュール実行）と手動実行用の Container App をデプロイし、Batch 処理の基盤を提供します。

## 概要

Container Jobs Bicep モジュールは、以下の機能を提供します：

- **スケジュール実行 Container Jobs**: データ登録と画像ダウンロードの定期バッチ処理
- **手動実行 Container App**: HTTP トリガーによる任意のタイミングでのバッチ処理実行
- **Managed Identity**: Key Vault と Storage へのセキュアなアクセス
- **Application Insights**: 自動統合によるモニタリング
- **環境変数設定**: PostgreSQL、Rakuten API Key、Storage の自動構成

## 使用方法

### 基本的な使用例

```bicep
module containerJobs 'modules/container-jobs.bicep' = {
  name: 'container-jobs-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'
    location: 'japaneast'
    projectName: 'comical'
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    postgresConnectionStringSecretUri: security.outputs.postgresConnectionStringSecretUri
    rakutenApiKeySecretUri: security.outputs.rakutenApiKeySecretUri
    tags: commonTags
  }
}
```

## パラメータ

| パラメータ名 | 型 | 必須 | 説明 | デフォルト値 |
|------------|-----|------|------|-------------|
| `environmentName` | string | Yes | 環境名 (dev, prod) | - |
| `location` | string | No | Azure リージョン | resourceGroup().location |
| `projectName` | string | Yes | プロジェクト名 | - |
| `storageAccountName` | string | Yes | Storage Account 名 | - |
| `appInsightsConnectionString` | string | No | Application Insights 接続文字列 | '' |
| `postgresConnectionStringSecretUri` | string | Yes | PostgreSQL 接続文字列シークレット URI | - |
| `rakutenApiKeySecretUri` | string | No | 楽天 API キーシークレット URI | '' |
| `tags` | object | No | リソースタグ | {} |

## 作成されるリソース

### 1. Container Apps Environment
- **名前**: `cae-comical-{env}-{location}`
- **用途**: Container Jobs と Container App の実行環境
- **Log Analytics**: 統合ログ記録

### 2. Log Analytics Workspace
- **名前**: `law-comical-{env}-{location}`
- **用途**: ログとメトリクスの集約
- **保持期間**:
  - 開発環境: 30 日
  - 本番環境: 90 日

### 3. データ登録 Container Job
- **名前**: `cjob-comical-datareg-{env}-{location}`
- **スケジュール**: 毎日 UTC 0:00 (JST 9:00)
- **Cron 式**: `0 0 * * *`
- **タイムアウト**: 14400 秒 (4 時間)
- **用途**: 楽天 API からの書籍データ登録
- **リソース**:
  - CPU: 0.5 コア
  - メモリ: 1 GiB
- **環境変数**:
  - `BATCH_JOB_TYPE`: `DataRegistration`
  - `DefaultConnection`: PostgreSQL 接続文字列（Key Vault 参照）
  - `RAKUTEN_API_KEY`: 楽天 API キー（Key Vault 参照）
  - `AzureWebJobsStorage`: Storage Account 接続文字列

### 4. 画像ダウンロード Container Job
- **名前**: `cjob-comical-imgdl-{env}-{location}`
- **スケジュール**: 毎日 UTC 4:00 (JST 13:00)
- **Cron 式**: `0 4 * * *`
- **タイムアウト**: 14400 秒 (4 時間)
- **用途**: 書籍画像のダウンロードと Storage への保存
- **リソース**:
  - CPU: 0.5 コア
  - メモリ: 1 GiB
- **環境変数**:
  - `BATCH_JOB_TYPE`: `ImageDownload`
  - `DefaultConnection`: PostgreSQL 接続文字列（Key Vault 参照）
  - `RAKUTEN_API_KEY`: 楽天 API キー（Key Vault 参照）
  - `AzureWebJobsStorage`: Storage Account 接続文字列

### 5. 手動実行 Container App
- **名前**: `ca-comical-manualbatch-{env}-{location}`
- **トリガー**: HTTP
- **アクセス**: External (外部アクセス可能)
- **ポート**: 8080
- **用途**: 任意のタイミングでバッチ処理を手動実行
- **スケーリング**:
  - 最小レプリカ: 0 (アイドル時は停止)
  - 最大レプリカ: 1
- **リソース**:
  - CPU: 0.5 コア
  - メモリ: 1 GiB

## Cron スケジュール設定

Container Jobs は標準的な Cron 式を使用してスケジュールを定義します。

### Cron 式の形式

```
分 時 日 月 曜日
```

### 設定されたスケジュール

| Job | Cron 式 | UTC 時刻 | JST 時刻 | 説明 |
|-----|---------|----------|----------|------|
| データ登録 | `0 0 * * *` | 毎日 0:00 | 毎日 9:00 | 楽天 API からデータ取得 |
| 画像ダウンロード | `0 4 * * *` | 毎日 4:00 | 毎日 13:00 | 画像ファイルをダウンロード |

### スケジュール設計の理由

1. **データ登録を先に実行**: 楽天 API からメタデータを取得
2. **4時間のインターバル**: データ登録完了後に画像ダウンロード開始
3. **楽天 API 制限考慮**: 1日のリクエスト制限内に収める
4. **各 Job に 4 時間のタイムアウト**: 大量データ処理に対応

## Managed Identity

すべての Container Jobs と Container App には System-assigned Managed Identity が設定されます。

### 必要な RBAC ロール

Security モジュールで以下のロールを付与する必要があります：

1. **Key Vault Secrets User**: シークレットの読み取り
2. **Storage Blob Data Contributor**: Blob への読み書き

### Managed Identity の出力

各 Job/App の Principal ID が出力され、Security モジュールで RBAC 設定に使用されます：

- `dataRegistrationJobPrincipalId`
- `imageDownloadJobPrincipalId`
- `manualBatchContainerAppPrincipalId`

## 環境変数設定

### 共通環境変数

すべての Container Jobs と Container App に設定される環境変数：

```yaml
AzureWebJobsStorage: <Storage Account 接続文字列>
APPLICATIONINSIGHTS_CONNECTION_STRING: <Application Insights 接続文字列>
DefaultConnection: <PostgreSQL 接続文字列 Key Vault URI>
RAKUTEN_API_KEY: <楽天 API キー Key Vault URI>
```

### Job 固有の環境変数

各 Container Job には、処理タイプを識別するための環境変数が追加されます：

- データ登録 Job: `BATCH_JOB_TYPE=DataRegistration`
- 画像ダウンロード Job: `BATCH_JOB_TYPE=ImageDownload`

## 手動実行 Container App の使用方法

### HTTP エンドポイント

デプロイ後、以下の URL で HTTP トリガーによる手動実行が可能です：

```bash
# 開発環境
https://ca-comical-manualbatch-dev-jpe.<region>.azurecontainerapps.io

# 本番環境
https://ca-comical-manualbatch-prod-jpe.<region>.azurecontainerapps.io
```

### 実行例

```bash
# 手動でバッチ処理を実行
curl -X POST https://ca-comical-manualbatch-dev-jpe.<region>.azurecontainerapps.io/api/batch \
  -H "Content-Type: application/json" \
  -d '{"jobType": "DataRegistration"}'
```

### 認証

外部アクセスが有効なため、必要に応じて認証機構を実装することを推奨します：

- API Key 認証
- Azure AD 認証
- Managed Identity ベースの認証

## モニタリング

### Log Analytics

すべてのログは Log Analytics Workspace に集約されます：

```bash
# ログの確認
az monitor log-analytics query \
  --workspace <workspace-id> \
  --analytics-query "ContainerAppConsoleLogs_CL | where ContainerName_s == 'data-registration' | order by TimeGenerated desc"
```

### Application Insights

Application Insights と統合され、以下の情報を監視できます：

- Job の実行時間
- 成功/失敗の状態
- エラーログとスタックトレース
- パフォーマンスメトリクス

### Azure Portal での確認

1. Azure Portal で Container Apps Environment を開く
2. "Jobs" タブで Job の実行履歴を確認
3. 各実行の詳細ログとメトリクスを表示

## トラブルシューティング

### Job が実行されない場合

1. **Cron 式の確認**:
   ```bash
   az containerapp job show \
     --name cjob-comical-datareg-dev-jpe \
     --resource-group rg-comical-d-jpe \
     --query "properties.configuration.scheduleTriggerConfig.cronExpression"
   ```

2. **Container Apps Environment の状態確認**:
   ```bash
   az containerapp env show \
     --name cae-comical-dev-jpe \
     --resource-group rg-comical-d-jpe \
     --query "properties.provisioningState"
   ```

3. **ログの確認**:
   ```bash
   az containerapp job execution list \
     --name cjob-comical-datareg-dev-jpe \
     --resource-group rg-comical-d-jpe
   ```

### タイムアウトエラーの対処

Job が 4 時間以内に完了しない場合：

1. **処理の最適化**: バッチサイズを調整
2. **リソースの増強**: CPU/メモリを増やす
3. **並列処理**: parallelism を増やす（現在は 1）

### 手動実行 Container App にアクセスできない場合

1. **Ingress 設定の確認**:
   ```bash
   az containerapp show \
     --name ca-comical-manualbatch-dev-jpe \
     --resource-group rg-comical-d-jpe \
     --query "properties.configuration.ingress"
   ```

2. **FQDN の確認**:
   ```bash
   az containerapp show \
     --name ca-comical-manualbatch-dev-jpe \
     --resource-group rg-comical-d-jpe \
     --query "properties.configuration.ingress.fqdn"
   ```

## セキュリティ考慮事項

### シークレット管理

- すべての機密情報は Key Vault に保存
- Container Jobs/Apps は Managed Identity で Key Vault にアクセス
- 接続文字列は Bicep の secrets セクションで管理

### ネットワークセキュリティ

- データ登録と画像ダウンロード Job は internal (予定)
- 手動実行 Container App は external (認証推奨)
- 将来的に VNet 統合を検討

### RBAC 権限

最小権限の原則に従い、各 Job/App に必要最小限の権限のみを付与：

- Key Vault: Secrets User (読み取りのみ)
- Storage: Blob Data Contributor (読み書き)

## Outputs

このモジュールは以下の値を出力します：

| Output 名 | 型 | 説明 |
|----------|-----|------|
| `containerAppsEnvironmentId` | string | Container Apps Environment のリソース ID |
| `containerAppsEnvironmentName` | string | Container Apps Environment 名 |
| `dataRegistrationJobId` | string | データ登録 Job のリソース ID |
| `dataRegistrationJobName` | string | データ登録 Job 名 |
| `dataRegistrationJobPrincipalId` | string | データ登録 Job の Managed Identity Principal ID |
| `imageDownloadJobId` | string | 画像ダウンロード Job のリソース ID |
| `imageDownloadJobName` | string | 画像ダウンロード Job 名 |
| `imageDownloadJobPrincipalId` | string | 画像ダウンロード Job の Managed Identity Principal ID |
| `manualBatchContainerAppId` | string | 手動実行 Container App のリソース ID |
| `manualBatchContainerAppName` | string | 手動実行 Container App 名 |
| `manualBatchContainerAppUrl` | string | 手動実行 Container App の URL |
| `manualBatchContainerAppPrincipalId` | string | 手動実行 Container App の Managed Identity Principal ID |
| `logAnalyticsWorkspaceId` | string | Log Analytics Workspace のリソース ID |

## 命名規則

Azure Cloud Adoption Framework (CAF) に準拠した命名規則：

### Container Job
```
cjob-{project}-{resource}-{env}-{location}
```

例:
- `cjob-comical-datareg-dev-jpe` (データ登録 Job、開発環境)
- `cjob-comical-imgdl-prod-jpe` (画像ダウンロード Job、本番環境)

### Container App (手動実行)
```
ca-{project}-{resource}-{env}-{location}
```

例:
- `ca-comical-manualbatch-dev-jpe` (手動実行、開発環境)
- `ca-comical-manualbatch-prod-jpe` (手動実行、本番環境)

### Container Apps Environment
```
cae-{project}-{env}-{location}
```

例:
- `cae-comical-dev-jpe` (開発環境)
- `cae-comical-prod-jpe` (本番環境)

### Log Analytics Workspace
```
law-{project}-{env}-{location}
```

例:
- `law-comical-dev-jpe` (開発環境)
- `law-comical-prod-jpe` (本番環境)

## ベストプラクティス

1. **スケジュールの設計**: API 制限と処理時間を考慮してスケジュールを設定
2. **タイムアウトの設定**: 十分な余裕を持った replicaTimeout を設定
3. **Managed Identity の活用**: シークレット管理には必ず Managed Identity を使用
4. **ログの監視**: Log Analytics と Application Insights でログを定期的に確認
5. **手動実行の活用**: トラブル時やテスト時に手動実行 Container App を使用

## 次のステップ

このモジュールをデプロイした後：

1. Security モジュールで RBAC ロールを割り当て
2. Container Image をビルドして Container Registry に push
3. Job の設定を更新して実際の Container Image を指定
4. 初回の Job 実行をテスト
5. Log Analytics でログを確認

## 参考資料

- [Azure Container Apps Jobs Documentation](https://learn.microsoft.com/azure/container-apps/jobs)
- [Container Apps Cron Expression](https://learn.microsoft.com/azure/container-apps/jobs?tabs=azure-cli#cron-expressions)
- [Azure Managed Identity](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [Azure Container Apps Best Practices](https://learn.microsoft.com/azure/container-apps/best-practices)

---

**最終更新日：** 2026-01-01
