# Functions Module

このモジュールは、ComiCal アプリケーション用の Azure Function Apps（API と Batch）をデプロイし、環境別のホスティングプラン、Application Insights 統合、および自動設定を提供します。

## 概要

Functions Bicep モジュールは、以下の機能を提供します：

- **環境別ホスティングプラン**: 開発環境では Consumption Plan、本番環境では Premium Plan
- **2つの Function Apps**: API 層と Batch 層を独立してデプロイ
- **Managed Identity**: Key Vault と Storage へのセキュアなアクセス
- **Application Insights**: 自動統合によるモニタリング
- **Application Settings**: Key Vault 参照による設定の自動構成

## 使用方法

### 基本的な使用例

```bicep
module functions 'modules/functions.bicep' = {
  name: 'functions-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'
    location: 'japaneast'
    projectName: 'comical'
    storageAccountName: storage.outputs.storageAccountName
    keyVaultUri: security.outputs.keyVaultUri
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
| `keyVaultUri` | string | No | Key Vault URI | '' |
| `postgresConnectionStringSecretUri` | string | Yes | PostgreSQL 接続文字列シークレット URI | - |
| `rakutenApiKeySecretUri` | string | No | 楽天 API キーシークレット URI | '' |
| `appInsightsConnectionString` | string | No | Application Insights 接続文字列（既存の場合） | '' |
| `tags` | object | No | リソースタグ | {} |

## 環境別設定

### 開発環境 (dev)

#### App Service Plan
- **名前**: `plan-comical-dev-jpe`
- **SKU**: Y1 (Consumption Plan - 従量課金)
- **特徴**:
  - 使用した分だけ課金
  - 自動スケーリング
  - Always On: 無効

#### API Function App
- **名前**: `func-comical-api-dev-jpe`
- **ランタイム**: .NET 8 Isolated
- **用途**: REST API エンドポイント

#### Batch Function App
- **名前**: `func-comical-batch-dev-jpe`
- **ランタイム**: .NET 8 Isolated
- **用途**: Durable Functions による定期バッチ処理

#### Application Insights
- **保持期間**: 30 日

### 本番環境 (prod)

#### App Service Plan
- **名前**: `plan-comical-prod-jpe`
- **SKU**: EP1 (Elastic Premium Plan)
- **特徴**:
  - 常時稼働
  - 予測可能な料金
  - VNet 統合対応
  - Always On: 有効

#### API Function App
- **名前**: `func-comical-api-prod-jpe`
- **ランタイム**: .NET 8 Isolated
- **用途**: REST API エンドポイント

#### Batch Function App
- **名前**: `func-comical-batch-prod-jpe`
- **ランタイム**: .NET 8 Isolated
- **用途**: Durable Functions による定期バッチ処理

#### Application Insights
- **保持期間**: 90 日

## 作成されるリソース

### 1. App Service Plan

- **命名規則**: `plan-{project}-{env}-{location}`
- **Linux ベース**: .NET Isolated ワーカー用

### 2. API Function App

- **命名規則**: `func-{project}-api-{env}-{location}`
- **機能**:
  - System-assigned Managed Identity
  - HTTPS のみ
  - CORS 有効
  - Linux コンテナ

### 3. Batch Function App

- **命名規則**: `func-{project}-batch-{env}-{location}`
- **機能**:
  - System-assigned Managed Identity
  - HTTPS のみ
  - Durable Functions サポート
  - Linux コンテナ

### 4. Application Insights

- **命名規則**: `appi-{project}-{env}-{location}`
- **自動作成**: 既存の Application Insights が指定されていない場合のみ

## Application Settings

両方の Function Apps に以下の設定が自動構成されます：

### 共通設定

```
AzureWebJobsStorage=<Storage Account 接続文字列>
WEBSITE_CONTENTAZUREFILECONNECTIONSTRING=<Storage Account 接続文字列>
WEBSITE_CONTENTSHARE=<Function App 名>
FUNCTIONS_EXTENSION_VERSION=~4
FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
APPLICATIONINSIGHTS_CONNECTION_STRING=<Application Insights 接続文字列>
ApplicationInsightsAgent_EXTENSION_VERSION=~3
XDT_MicrosoftApplicationInsights_Mode=recommended
DefaultConnection=@Microsoft.KeyVault(SecretUri=<PostgreSQL 接続文字列シークレット URI>)
StorageAccountName=<Storage Account 名>
```

### Batch Function App 専用設定

```
RakutenBooksApi__applicationid=@Microsoft.KeyVault(SecretUri=<楽天 API キーシークレット URI>)
```

## Key Vault 参照

Application Settings では Key Vault 参照を使用してシークレットを安全に取得します：

```
@Microsoft.KeyVault(SecretUri=https://kv-comical-dev-jpe.vault.azure.net/secrets/PostgresConnectionString)
```

Function Apps の Managed Identity が Key Vault にアクセスするため、Security モジュールで RBAC 権限が自動的に付与されます。

## Managed Identity と RBAC

各 Function App には System-assigned Managed Identity が有効化され、以下の権限が付与されます（Security モジュール経由）：

| Function App | リソース | ロール |
|-------------|---------|--------|
| API Function App | Key Vault | Key Vault Secrets User |
| API Function App | Storage Account | Storage Blob Data Contributor |
| Batch Function App | Key Vault | Key Vault Secrets User |
| Batch Function App | Storage Account | Storage Blob Data Contributor |

## Durable Functions サポート

Batch Function App は Durable Functions をサポートします：

- **ストレージ**: `AzureWebJobsStorage` を使用
- **タスクハブ**: 自動設定
- **用途**: 楽天ブックス API からのデータ取得とバッチ処理

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `appServicePlanId` | string | App Service Plan のリソース ID |
| `appServicePlanName` | string | App Service Plan 名 |
| `appServicePlanSku` | string | App Service Plan SKU |
| `apiFunctionAppId` | string | API Function App のリソース ID |
| `apiFunctionAppName` | string | API Function App 名 |
| `apiFunctionAppPrincipalId` | string | API Function App の Managed Identity プリンシパル ID |
| `apiFunctionAppHostname` | string | API Function App のホスト名 |
| `batchFunctionAppId` | string | Batch Function App のリソース ID |
| `batchFunctionAppName` | string | Batch Function App 名 |
| `batchFunctionAppPrincipalId` | string | Batch Function App の Managed Identity プリンシパル ID |
| `batchFunctionAppHostname` | string | Batch Function App のホスト名 |
| `appInsightsConnectionString` | string | Application Insights 接続文字列 |
| `appInsightsInstrumentationKey` | string | Application Insights インストルメンテーションキー |

## デプロイ

### GitHub Actions でのデプロイ

`.github/workflows/api-functions-deploy.yml` と `.github/workflows/batch-functions-deploy.yml` を参照してください。

### 手動デプロイ

```bash
# API Function App
cd src/ComiCal.Server/Comical.Api
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
cd publish
zip -r ../api.zip .
az functionapp deployment source config-zip \
  --resource-group rg-comical-d-jpe \
  --name func-comical-api-dev-jpe \
  --src ../api.zip

# Batch Function App
cd src/ComiCal.Server/ComiCal.Batch
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
cd publish
zip -r ../batch.zip .
az functionapp deployment source config-zip \
  --resource-group rg-comical-d-jpe \
  --name func-comical-batch-dev-jpe \
  --src ../batch.zip
```

## 夜間停止（開発環境のみ）

開発環境の Function Apps は、Cost Optimization モジュールによって以下のスケジュールで自動停止されます：

- **平日**: 20:00-08:00 JST
- **週末**: 土日終日停止

詳細は [Cost Optimization モジュール](./COST_OPTIMIZATION.md) を参照してください。

## セキュリティ考慮事項

1. **Managed Identity**
   - すべてのシークレットアクセスは Managed Identity 経由
   - アクセスキーやパスワードをコードに含めない

2. **HTTPS のみ**
   - HTTP トラフィックは許可されません
   - TLS 1.2 以上

3. **CORS**
   - 開発環境ではすべてのオリジンを許可
   - 本番環境では特定のドメインのみを許可するよう変更を推奨

4. **Key Vault 参照**
   - Application Settings で機密情報を直接設定しない
   - すべてのシークレットは Key Vault 参照を使用

## コスト最適化

### 開発環境

- **Consumption Plan**: 使用した分だけ課金
- **夜間停止**: Logic Apps による自動停止でコスト削減
- **Application Insights**: 30 日保持で最小化

### 本番環境

- **Premium Plan**: 予測可能な料金
- **Always On**: 常時稼働で高パフォーマンス
- **Application Insights**: 90 日保持

## トラブルシューティング

### Key Vault アクセスエラー

Function Apps が Key Vault にアクセスできない場合：

```bash
# RBAC 権限を確認
az role assignment list \
  --assignee <function-app-principal-id> \
  --scope <key-vault-id>
```

### Storage アクセスエラー

```bash
# RBAC 権限を確認
az role assignment list \
  --assignee <function-app-principal-id> \
  --scope <storage-account-id>
```

### ログの確認

```bash
# Function App ログを表示
az functionapp log tail \
  --resource-group rg-comical-d-jpe \
  --name func-comical-api-dev-jpe

# Application Insights でログをクエリ
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where timestamp > ago(1h) | order by timestamp desc"
```

## 関連ドキュメント

- [Azure Functions ドキュメント](https://docs.microsoft.com/azure/azure-functions/)
- [Consumption Plan vs Premium Plan](https://docs.microsoft.com/azure/azure-functions/functions-scale)
- [Managed Identity の使用](https://docs.microsoft.com/azure/app-service/overview-managed-identity)
- [Key Vault 参照](https://docs.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Durable Functions](https://docs.microsoft.com/azure/azure-functions/durable/)

## 次のステップ

1. Function Apps にコードをデプロイ
2. API エンドポイントをテスト
3. Batch 処理を実行してデータ取得を確認
4. Application Insights でパフォーマンスを監視

---

**最終更新日：** 2025-12-31
