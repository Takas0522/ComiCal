# デプロイチェックリスト

このドキュメントでは、ComiCal アプリケーションをAzure環境にデプロイする際のチェックリストと構成手順を説明します。

## 前提条件

- Azure サブスクリプション
- Azure CLI がインストールされていること
- 以下のAzureリソースが作成済みであること：
  - Azure Functions (API層とBatch層)
  - Azure Blob Storage
  - Azure PostgreSQL Database
  - Azure Static Web Apps (フロントエンド)

## Azure構成

### 1. Function App構成

Azure Functions を .NET 10 LTS + Isolated worker model で動作させるための基本設定を行います。

#### 1.1 ワーカープロセスモデルの設定

Azure Portal での設定手順：

1. **Function App** → 対象のFunction Appを選択
2. **構成** → **全般設定** タブ
3. **ワーカープロセス**: `分離` を選択
4. 保存

#### 1.2 ランタイム設定

**Application Settings** に以下を追加：

| 設定名 | 値 | 説明 |
|--------|-----|------|
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | .NET Isolated worker model を使用 |
| `FUNCTIONS_EXTENSION_VERSION` | `~4` | Azure Functions v4 を使用 |

Azure CLI での設定例：

```bash
# 変数を設定
RESOURCE_GROUP="<your-resource-group>"
FUNCTION_APP_NAME="<your-function-app-name>"

# ランタイム設定
az functionapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --settings FUNCTIONS_WORKER_RUNTIME=dotnet-isolated FUNCTIONS_EXTENSION_VERSION=~4
```

### 2. Managed Identity有効化

セキュリティのベストプラクティスとして、接続文字列の代わりにManaged Identityを使用します。

#### 2.1 システム割り当てManaged Identityの有効化

Azure Portal での設定手順：

1. **Function App** → 対象のFunction Appを選択
2. **ID** → **システム割り当て** タブ
3. **状態**: `オン` に変更
4. 保存

Azure CLI での設定例：

```bash
# システム割り当てManaged Identityを有効化
az functionapp identity assign \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME

# プリンシパルIDを取得（RBAC設定で使用）
PRINCIPAL_ID=$(az functionapp identity show \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --query principalId \
  --output tsv)

echo "Principal ID: $PRINCIPAL_ID"
```

### 3. Application Settings

#### 3.1 画像ストレージ設定（静的リソースホスティング用）

画像を集約するBlob Storageは、静的ウェブサイトホスティング機能を使用してパブリックアクセスを提供します。
Function Appsからのアクセスは想定していません。

**静的ウェブサイトホスティング設定**:
- Azure Portal → ストレージアカウント → **静的ウェブサイト** → **有効化**
- コンテナー名: `$web`（自動作成）
- パブリック読み取りアクセスを許可

**フロントエンド（Static Web Apps）での設定**:

| 設定名 | 値 | 説明 |
|--------|-----|------|
| `blobBaseUrl` | `https://<storage-account-name>.z11.web.core.windows.net/` | 画像ストレージの静的ウェブサイトエンドポイント |

Azure CLI での設定例：

```bash
# 画像ストレージアカウントで静的ウェブサイトホスティングを有効化
IMAGE_STORAGE_ACCOUNT_NAME="<your-image-storage-account-name>"

# 静的ウェブサイトホスティングを有効化
az storage blob service-properties update \
  --account-name $IMAGE_STORAGE_ACCOUNT_NAME \
  --static-website \
  --index-document index.html \
  --404-document 404.html

# フロントエンド（Static Web Apps）に画像ベースURL設定
STATIC_WEB_APP_NAME="<your-static-web-app-name>"
IMAGE_BASE_URL="https://${IMAGE_STORAGE_ACCOUNT_NAME}.z11.web.core.windows.net/"

az staticwebapp appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $STATIC_WEB_APP_NAME \
  --setting-names blobBaseUrl=$IMAGE_BASE_URL
```

**画像ストレージの特徴**:
- パブリック読み取りアクセス許可
- 静的ウェブサイトホスティング機能使用
- Function Appsからの直接アクセスなし（管理用途を除く）
- CDN併用でパフォーマンス向上可能

#### 3.2 PostgreSQL接続文字列

| 設定名 | 接続文字列形式 |
|--------|----------------|
| `DefaultConnection` (ConnectionStrings) | `Host=<server>.postgres.database.azure.com;Database=comical;Username=<user>;Password=<password>;SslMode=Require` |

**Managed Identity を使用する場合（推奨）**:

接続文字列でPasswordを省略すると、Npgsqlは自動的にAzure AD認証を試行します：
```
Host=<server>.postgres.database.azure.com;Database=comical;Username=<managed-identity-name>;SslMode=Require
```

**注意**: 
- PostgreSQL側でManaged IdentityをAzure ADユーザーとして登録する必要があります
- `<managed-identity-name>`はFunction AppのManaged Identity名と一致させます
  - システム割り当てManaged Identityの場合、通常はFunction App名と同じになります
  - 例: Function App名が `comical-api-prod` の場合、Managed Identity名も `comical-api-prod` となります

Azure CLI での設定例：

```bash
# 接続文字列を設定（パスワード認証）
POSTGRES_CONNECTION="Host=<server>.postgres.database.azure.com;Database=comical;Username=<user>;Password=<password>;SslMode=Require"

az functionapp config connection-string set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection=$POSTGRES_CONNECTION

# Managed Identity認証の場合（Passwordを省略）
# 注：PostgreSQL側での事前設定が必要
POSTGRES_CONNECTION_MI="Host=<server>.postgres.database.azure.com;Database=comical;Username=<managed-identity-name>;SslMode=Require"

az functionapp config connection-string set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection=$POSTGRES_CONNECTION_MI
```

#### 3.3 Batch層固有の設定

Batch層（Durable Functions）では、楽天ブックスAPIの認証情報が必要です。

| 設定名 | 値 | 説明 |
|--------|-----|------|
| `applicationid` | `<your-rakuten-app-id>` | 楽天ブックスAPI ApplicationID |

Azure CLI での設定例：

```bash
az functionapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --settings applicationid=<your-rakuten-app-id>
```

### 4. RBAC設定

#### 4.1 画像ストレージのアクセス権限

画像ストレージは静的ウェブサイトホスティング機能を使用するため、**パブリック読み取りアクセスが必要**です。
Function AppsからのManaged Identity認証は**不要**です。

Azure CLI での設定例：

```bash
# 画像ストレージアカウントでパブリック読み取りアクセスを許可
az storage account update \
  --name $IMAGE_STORAGE_ACCOUNT_NAME \
  --resource-group $RESOURCE_GROUP \
  --allow-blob-public-access true

# $web コンテナー（静的ウェブサイト用）のアクセスレベルを確認
az storage container show \
  --account-name $IMAGE_STORAGE_ACCOUNT_NAME \
  --name '$web' \
  --query 'properties.publicAccess' \
  --output tsv
```

#### 4.2 管理用のRBAC設定（オプション）

画像のアップロードや管理を行う場合のみ、適切なユーザーまたはサービスプリンシパルにRBACロールを付与：

```bash
# 管理者にStorage Blob Data Contributorロールを付与（画像管理用）
# Function Appsには不要
IMAGE_STORAGE_ACCOUNT_ID=$(az storage account show \
  --name $IMAGE_STORAGE_ACCOUNT_NAME \
  --resource-group $RESOURCE_GROUP \
  --query id \
  --output tsv)

# 管理用ユーザーにロール付与
az role assignment create \
  --assignee <admin-user-or-service-principal> \
  --role "Storage Blob Data Contributor" \
  --scope $IMAGE_STORAGE_ACCOUNT_ID
```

### 5. Durable Functions設定

#### 5.1 AzureWebJobsStorage

Durable Functionsの互換性のため、`AzureWebJobsStorage` は**接続文字列形式を継続**します。

| 設定名 | 値 | 説明 |
|--------|-----|------|
| `AzureWebJobsStorage` | `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net` | Durable Functionsのオーケストレーション状態管理に使用 |

**重要**: 
- `AzureWebJobsStorage` は Durable Functions の内部状態管理に使用されるため、接続文字列形式が必要です
- `StorageAccountName` による Managed Identity 認証は、アプリケーションコードからのBlob操作にのみ適用されます

Azure CLI での設定例：

```bash
# AzureWebJobsStorage を接続文字列形式で設定
# ⚠️ 重要なセキュリティ注意事項:
# - 実際の Account Key は環境変数や Azure Key Vault から読み込んでください
# - 接続文字列をコードやスクリプトにハードコードしないでください
# - バージョン管理システムに機密情報をコミットしないでください

# 推奨: 環境変数から読み込む
AZUREWEBJOBS_STORAGE="DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net"

az functionapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --settings AzureWebJobsStorage=$AZUREWEBJOBS_STORAGE

# または Azure Key Vault 参照を使用（最も安全）
# versionless形式（最新バージョンを自動取得）
# AzureWebJobsStorage="@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/AzureWebJobsStorage/)"
# versioned形式（特定バージョンを指定）
# AzureWebJobsStorage="@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/AzureWebJobsStorage/<version>)"
```

#### 5.2 Batch層のスケジュール設定

TimerTriggerのスケジュール（`0 0 0 * * *` = UTC 0:00）は `function.json` または属性で定義されています。
Azure環境では UTC 0:00～0:05 のみ実行するガードロジックが実装されています。

## セキュリティベストプラクティス

### 1. Managed Identityの使用

✅ **推奨**: Azure リソース間の認証には Managed Identity を使用
- 接続文字列にシークレットを含めない
- 自動的にローテーションされる資格情報
- Azure Key Vault と組み合わせることでさらにセキュアに

❌ **非推奨**: 接続文字列に直接パスワードやキーを含める

### 2. 最小権限の原則

各 Managed Identity には必要最小限のRBACロールのみを付与：
- Blob操作のみ → `Storage Blob Data Contributor`
- 読み取りのみ → `Storage Blob Data Reader`

### 3. Application Settingsの管理

機密情報は Azure Key Vault に保存し、Key Vault参照を使用することを推奨：

```bash
# Key Vault 参照の例（versionless形式 - 最新バージョンを自動取得）
@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/<secret-name>/)

# Key Vault 参照の例（特定バージョンを指定）
@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/<secret-name>/<version>)
```

## デプロイ後の確認

### 1. Function Appの動作確認

```bash
# Function App のステータス確認
az functionapp show \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --query state \
  --output tsv
```

### 2. Application Settings の確認

```bash
# すべての設定を表示
az functionapp config appsettings list \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --output table
```

### 3. Managed Identity の確認

```bash
# Managed Identity の状態確認
az functionapp identity show \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME
```

### 4. ログの確認

Azure Portal で以下を確認：
1. **Function App** → **ログストリーム**
2. Application Insights でエラーや警告がないか確認

## トラブルシューティング

### Managed Identity 認証エラー

**症状**: Blob Storage へのアクセスで認証エラーが発生

**確認項目**:
1. System Assigned Managed Identity が有効化されているか
2. `StorageAccountName` が正しく設定されているか
3. RBAC ロール (`Storage Blob Data Contributor`) が付与されているか
4. ロール付与の反映には数分かかる場合があります（最大5分程度待機）

```bash
# エラーログの確認
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where message contains 'Storage' | order by timestamp desc | take 20"
```

### Durable Functions が動作しない

**症状**: オーケストレーションが開始されない、または状態が保存されない

**確認項目**:
1. `AzureWebJobsStorage` が接続文字列形式で設定されているか
2. ストレージアカウントへのネットワークアクセスが可能か
3. Function App の FUNCTIONS_EXTENSION_VERSION が `~4` に設定されているか

### PostgreSQL 接続エラー

**症状**: データベース接続で認証エラーが発生

**確認項目**:
1. `DefaultConnection` (ConnectionStrings) が正しく設定されているか
2. PostgreSQL サーバーのファイアウォール設定でAzure サービスを許可しているか
3. Managed Identity を使用する場合、PostgreSQL に適切なユーザーが作成されているか

## 参考リンク

- [Azure Functions - Isolated worker model](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Managed identities for Azure resources](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview)
- [Azure Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview)
- [Azure Blob Storage - Managed Identity 認証](https://learn.microsoft.com/azure/storage/common/authorize-data-access)
- [Azure Key Vault references for App Service and Azure Functions](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)

## デプロイチェックリスト

実際にデプロイする際は、以下のチェックリストを使用してください：

### API層 (Function App)

- [ ] Function App が作成されている
- [ ] ワーカープロセスモデルが「分離」に設定されている
- [ ] `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` が設定されている
- [ ] システム割り当てManaged Identityが有効化されている
- [ ] `DefaultConnection` (PostgreSQL) が設定されている
- [ ] `AzureWebJobsStorage` が接続文字列形式で設定されている

### Batch層 (Function App)

- [ ] Function App が作成されている
- [ ] ワーカープロセスモデルが「分離」に設定されている
- [ ] `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` が設定されている
- [ ] システム割り当てManaged Identityが有効化されている
- [ ] `DefaultConnection` (PostgreSQL) が設定されている
- [ ] `AzureWebJobsStorage` が接続文字列形式で設定されている
- [ ] `applicationid` (楽天ブックスAPI) が設定されている

### フロントエンド (Static Web Apps)

- [ ] Static Web Apps が作成されている
- [ ] カスタムドメインが設定されている（必要な場合）
- [ ] API の CORS 設定が正しく構成されている
- [ ] 環境変数 (`blobBaseUrl` 等) が設定されている

### 画像ストレージ (Blob Storage)

- [ ] 画像用ストレージアカウントが作成されている
- [ ] 静的ウェブサイトホスティングが有効化されている
- [ ] パブリック読み取りアクセスが許可されている
- [ ] `$web` コンテナーが作成されている
- [ ] フロントエンドに `blobBaseUrl` が正しく設定されている
- [ ] CDNが設定されている（パフォーマンス向上のため、オプション）

### 全体

- [ ] すべてのリソースが同一リージョンに配置されている（推奨）
- [ ] Application Insights が有効化されている
- [ ] アラート設定が構成されている
- [ ] バックアップ戦略が確立されている（データベース、ストレージ）
- [ ] デプロイ後の動作確認が完了している
