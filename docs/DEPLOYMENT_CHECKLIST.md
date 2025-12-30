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

#### 3.1 Blob Storage設定（Managed Identity使用）

API層とBatch層の両方で以下の設定を行います。

| 設定名 | 値 | 説明 |
|--------|-----|------|
| `StorageAccountName` | `<storage-account-name>` | ストレージアカウント名（例: `comicalstorageprod`）<br>この設定があると Managed Identity 認証が優先されます |
| `StorageConnectionString` | `DefaultEndpointsProtocol=https;...` | **フォールバック用に保持**<br>Managed Identity 認証が失敗した場合の代替手段 |

Azure CLI での設定例：

```bash
# ストレージアカウント名を設定（Managed Identity認証を有効化）
STORAGE_ACCOUNT_NAME="<your-storage-account-name>"

az functionapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --settings StorageAccountName=$STORAGE_ACCOUNT_NAME

# StorageConnectionString をフォールバック用に設定（オプション）
# セキュリティ上の理由から、可能であればManaged Identityのみを使用することを推奨
STORAGE_CONNECTION_STRING="<your-storage-connection-string>"
az functionapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --settings StorageConnectionString=$STORAGE_CONNECTION_STRING
```

**認証の優先順位**:
1. `StorageAccountName` が設定されている場合 → Managed Identity (`DefaultAzureCredential`)
2. `StorageAccountName` が未設定の場合 → `StorageConnectionString` を使用

#### 3.2 PostgreSQL接続文字列

| 設定名 | 接続文字列形式 |
|--------|----------------|
| `DefaultConnection` (ConnectionStrings) | `Host=<server>.postgres.database.azure.com;Database=comical;Username=<user>;Password=<password>;SslMode=Require` |

Managed Identity を使用する場合（推奨）：
```
Host=<server>.postgres.database.azure.com;Database=comical;Username=<managed-identity-name>;SslMode=Require
```

Azure CLI での設定例：

```bash
# 接続文字列を設定（パスワード認証）
POSTGRES_CONNECTION="Host=<server>.postgres.database.azure.com;Database=comical;Username=<user>;Password=<password>;SslMode=Require"

az functionapp config connection-string set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection=$POSTGRES_CONNECTION
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

Managed Identityに適切なRBACロールを付与します。

#### 4.1 Storage Blob Data Contributorロールの付与

Function AppのManaged IdentityにBlobへの読み書き権限を付与します。

Azure CLI での設定例：

```bash
# ストレージアカウントのリソースIDを取得
STORAGE_ACCOUNT_ID=$(az storage account show \
  --name $STORAGE_ACCOUNT_NAME \
  --resource-group $RESOURCE_GROUP \
  --query id \
  --output tsv)

# Storage Blob Data Contributor ロールを付与
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Contributor" \
  --scope $STORAGE_ACCOUNT_ID

echo "RBAC role 'Storage Blob Data Contributor' assigned successfully"
```

#### 4.2 ロール付与の確認

```bash
# ロール割り当ての確認
az role assignment list \
  --assignee $PRINCIPAL_ID \
  --scope $STORAGE_ACCOUNT_ID \
  --output table
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
AZUREWEBJOBS_STORAGE="DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net"

az functionapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --settings AzureWebJobsStorage=$AZUREWEBJOBS_STORAGE
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
# Key Vault 参照の例
@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/<secret-name>/)
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
- [Azure Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [Azure Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview)
- [Azure Blob Storage - Managed Identity 認証](https://learn.microsoft.com/azure/storage/common/authorize-data-access)

## デプロイチェックリスト

実際にデプロイする際は、以下のチェックリストを使用してください：

### API層 (Function App)

- [ ] Function App が作成されている
- [ ] ワーカープロセスモデルが「分離」に設定されている
- [ ] `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` が設定されている
- [ ] システム割り当てManaged Identityが有効化されている
- [ ] `StorageAccountName` が設定されている
- [ ] Storage Blob Data Contributor ロールが付与されている
- [ ] `DefaultConnection` (PostgreSQL) が設定されている
- [ ] `AzureWebJobsStorage` が接続文字列形式で設定されている

### Batch層 (Function App)

- [ ] Function App が作成されている
- [ ] ワーカープロセスモデルが「分離」に設定されている
- [ ] `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` が設定されている
- [ ] システム割り当てManaged Identityが有効化されている
- [ ] `StorageAccountName` が設定されている
- [ ] Storage Blob Data Contributor ロールが付与されている
- [ ] `DefaultConnection` (PostgreSQL) が設定されている
- [ ] `AzureWebJobsStorage` が接続文字列形式で設定されている
- [ ] `applicationid` (楽天ブックスAPI) が設定されている

### フロントエンド (Static Web Apps)

- [ ] Static Web Apps が作成されている
- [ ] カスタムドメインが設定されている（必要な場合）
- [ ] API の CORS 設定が正しく構成されている
- [ ] 環境変数 (`blobBaseUrl` 等) が設定されている

### 全体

- [ ] すべてのリソースが同一リージョンに配置されている（推奨）
- [ ] Application Insights が有効化されている
- [ ] アラート設定が構成されている
- [ ] バックアップ戦略が確立されている（データベース、ストレージ）
- [ ] デプロイ後の動作確認が完了している
