# まんがリマインダー(α)

## 展開先

https://manrem.devtakas.jp/

## 構成

![](./.attachements/2021-08-22-15-47-09.png)

# 開発について

自分が別環境で開発するときの備忘録的な…

## 開発環境

- @angular/cli
  - ^13.0.0
- Azure Functions Core Tools
- @azure/static-web-apps-cli
- VisualStudio
  - Visual Studio CodeでもOK
- SQL Server
  - localdbでOK
- Azure Cosmos DB（サーバーレスモード推奨）
- Azure Blob Storage
- Azure CLI（セットアップ用）

## Web開発

### 初期セットアップ

#### 1. Cosmos DB のセットアップ

Cosmos DB データベースとコンテナを作成します：

```powershell
# Azure CLI でログイン（初回のみ）
az login

# セットアップスクリプトを実行
cd scripts
.\setup-cosmosdb.ps1 -CosmosAccountName "<your-cosmos-account-name>" -ResourceGroupName "<your-resource-group-name>"
```

スクリプトは以下を自動的に作成します：
- データベース: `ComiCalDB`
- コンテナ: `comics`（パーティションキー: `/id`、インデックス最適化済み）
- コンテナ: `config-migrations`（パーティションキー: `/id`）

スクリプト実行後、表示される接続文字列を `api/Comical.Api/local.settings.json` に設定してください：

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "CosmosConnectionString": "<表示された接続文字列>"
  }
}
```

#### 2. Blob Storage のセットアップ

Azure Portal または Azure CLI で Blob Storage アカウントを作成し、接続文字列を `local.settings.json` に追加してください：

```json
{
  "Values": {
    "StorageConnectionString": "<your-blob-storage-connection-string>"
  }
}
```

### ローカル開発実行

1. apiデバッグ実行/apiディレクトリで`func start`
2. frontディレクトリで`npm run start`
3. frontディレクトリで`npm run start:swa`
4. http://localhost:4280