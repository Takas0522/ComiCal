# セットアップスクリプト

このディレクトリには、ComiCal アプリケーションのセットアップに必要なスクリプトが含まれています。

## setup-cosmosdb.ps1

Cosmos DB データベースとコンテナを初期化するための PowerShell スクリプトです。

### 前提条件

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) がインストールされていること
- Azure にログイン済みであること（`az login`）
- Cosmos DB アカウントが作成済みであること（サーバーレスモード推奨）

### 使用方法

```powershell
.\setup-cosmosdb.ps1 -CosmosAccountName "<your-cosmos-account-name>" -ResourceGroupName "<your-resource-group-name>"
```

### パラメータ

- **CosmosAccountName** (必須): Cosmos DB アカウント名
- **ResourceGroupName** (必須): Azure リソースグループ名
- **DatabaseName** (オプション): データベース名（デフォルト: `ComiCalDB`）

### スクリプトが実行する処理

1. Azure CLI の存在確認
2. Azure アカウントの認証状態確認
3. Cosmos DB アカウントの存在確認
4. データベース `ComiCalDB` の作成（存在しない場合）
5. `comics` コンテナの作成（存在しない場合）
   - パーティションキー: `/id`
   - インデックスポリシー:
     - すべてのプロパティに範囲インデックス（ワイルドカードパス `/*`）
     - `/imageBaseUrl` を除外（将来の使用に備えた最適化）
     - `/_etag` を除外（システムプロパティ）
6. `config-migrations` コンテナの作成（存在しない場合）
   - パーティションキー: `/id`
   - デフォルトインデックスポリシー
7. 接続文字列の取得と表示

### 実行例

```powershell
# Cosmos DB アカウントが mycosmosdb、リソースグループが my-rg の場合
.\setup-cosmosdb.ps1 -CosmosAccountName "mycosmosdb" -ResourceGroupName "my-rg"

# カスタムデータベース名を指定する場合
.\setup-cosmosdb.ps1 -CosmosAccountName "mycosmosdb" -ResourceGroupName "my-rg" -DatabaseName "MyCustomDB"
```

### 出力

スクリプトは実行後、以下の情報を表示します：

- 作成されたデータベースとコンテナの情報
- Cosmos DB 接続文字列（アプリケーション設定に必要）

接続文字列を `src/ComiCal.Server/Comical.Api/local.settings.json` に設定してください：

```json
{
  "Values": {
    "CosmosConnectionString": "<スクリプトが表示した接続文字列>"
  }
}
```

### トラブルシューティング

- **Azure CLI が見つからない**: [Azure CLI をインストール](https://docs.microsoft.com/cli/azure/install-azure-cli)してください
- **認証エラー**: `az login` を実行してログインしてください
- **Cosmos DB アカウントが見つからない**: アカウント名とリソースグループ名が正しいか確認してください
- **権限エラー**: Azure サブスクリプションで適切な権限（Contributor 以上）があるか確認してください

### 注意事項

- このスクリプトは既存のリソースを変更しません（冪等性を保証）
- 既に存在するコンテナはスキップされます
- サーバーレスモードの Cosmos DB アカウントの使用を推奨します
