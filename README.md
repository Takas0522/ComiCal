# まんがリマインダー(α)

## 展開先

https://manrem.devtakas.jp/

## 構成

### アーキテクチャ概要

```
┌─────────────┐
│  Frontend   │
│  (Angular)  │  ← Static Web Apps
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────┐       ┌──────────────────┐
│  API Layer  │◄─────►│   PostgreSQL     │
│ (Functions) │       │  - comics        │
└──────┬──────┘       │  - config-migr...│
       │              └──────────────────┘
       │
┌──────▼──────┐       ┌──────────────┐
│ Batch Layer │◄─────►│ Rakuten API  │
│ (Functions) │       └──────────────┘
└──────┬──────┘
       │
       ▼
┌─────────────┐
│Blob Storage │  ← 画像保存
│  - images   │
└─────────────┘
```

**主要技術スタック**:
- **フロントエンド**: Angular 17, Azure Static Web Apps
- **API**: Azure Functions (.NET 10 LTS + Isolated worker model), PostgreSQL
- **Batch**: Azure Durable Functions (.NET 10 LTS + Isolated worker model), Blob Storage
- **外部API**: 楽天ブックスAPI

**データフロー**:
1. Batch層が楽天APIから漫画データを取得
2. PostgreSQLに保存、画像はBlob Storageに保存
3. ユーザーがフロントエンドで検索
4. API層がPostgreSQLからデータを取得して返却
5. フロントエンドが画像をBlob Storageから動的に読み込み

詳細は [アーキテクチャ図](./.attachements/2021-08-22-15-47-09.png) を参照。

# 開発について

自分が別環境で開発するときの備忘録的な…

## 開発環境

- @angular/cli
  - ^13.0.0
- Azure Functions Core Tools
- @azure/static-web-apps-cli
- VisualStudio
  - Visual Studio CodeでもOK
- PostgreSQL 14+
  - Dockerでの起動推奨
- Azure Database for PostgreSQL Flexible Server（本番環境）
- Azure Blob Storage
- Azure CLI（セットアップ用）

## Web開発

### 初期セットアップ

#### 1. PostgreSQL のセットアップ

**ローカル開発環境（Docker使用）**:

```bash
# PostgreSQLコンテナを起動
docker run --name comical-postgres \
  -e POSTGRES_DB=comical \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=password \
  -p 5432:5432 \
  -d postgres:14

# データベースの初期化（マイグレーション実行）
# NOTE: マイグレーションスクリプトは Issue #90 の完了後に利用可能になります
# 現時点ではPostgreSQLの起動のみ実施してください
```

**Azure環境**:

```bash
# Azure Database for PostgreSQL Flexible Serverを作成
az postgres flexible-server create \
  --resource-group <your-resource-group> \
  --name <your-server-name> \
  --location japaneast \
  --admin-user adminuser \
  --admin-password <your-password> \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --version 14

# データベースを作成
az postgres flexible-server db create \
  --resource-group <your-resource-group> \
  --server-name <your-server-name> \
  --database-name comical
```

**Managed Identity設定（本番環境推奨）**:

Azure Functions に Managed Identity を設定し、PostgreSQL へのアクセスを許可します：

```bash
# Functions AppにManaged Identityを有効化
az functionapp identity assign \
  --name <your-function-app> \
  --resource-group <your-resource-group>

# PostgreSQLでManaged Identityを認証ユーザーとして追加
# Azure PortalのPostgreSQL > Authentication > Add Azure AD Adminで設定
```

Managed Identity使用時は、接続文字列に資格情報を含める必要がありません：
```
Host=<server>.postgres.database.azure.com;Database=comical;Username=<managed-identity-name>
```

#### 2. Blob Storage のセットアップ

Azure Portal または Azure CLI で Blob Storage アカウントを作成し、コンテナ `images` を作成してください。

#### 3. 設定ファイルのセットアップ

テンプレートファイルをコピーして、実際の接続文字列を設定します：

**API層の設定** (`src/api/local.settings.json`):
```bash
# テンプレートからコピー
cp src/api/local.settings.json.template src/api/local.settings.json
```

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PostgresConnectionString": "Host=localhost;Port=5432;Database=comical;Username=postgres;Password=password",
    "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=<storage-account>;AccountKey=<your-key>;EndpointSuffix=core.windows.net"
  }
}
```

**Batch層の設定** (`src/batch/local.settings.json`):
```bash
# テンプレートからコピー
cp src/batch/local.settings.json.template src/batch/local.settings.json
```
設定内容は API 層と同じです。

**フロントエンド環境設定** (`front/src/environments/environment.ts`):
```typescript
export const environment = {
  production: false,
  gapiClientId: '<your-google-client-id>',
  blobBaseUrl: 'https://<storage-account>.blob.core.windows.net/images'
};
```

**環境変数一覧**:

| 変数名 | 説明 | 例 |
|--------|------|-----|
| `PostgresConnectionString` | PostgreSQL 接続文字列（ローカル開発） | `Host=localhost;Port=5432;Database=comical;Username=postgres;Password=password` |
| `PostgresConnectionString` | PostgreSQL 接続文字列（Azure with Managed Identity） | `Host=<server>.postgres.database.azure.com;Database=comical;Username=<managed-identity-name>` |
| `StorageConnectionString` | Blob Storage 接続文字列 | `DefaultEndpointsProtocol=https;AccountName=...` |
| `blobBaseUrl` | Blob Storage の画像ベースURL | `https://<account>.blob.core.windows.net/images` |

> **セキュリティ注意**: 本番環境では、接続文字列にパスワードを含めるのではなく、Azure Managed Identity を使用することを強く推奨します。これにより、設定ファイルに機密情報を保存する必要がなくなり、自動的にローテーションされる資格情報を使用できます。

#### .NET 10 Isolated移行後の設定

プロジェクトは .NET 10 LTS と Isolated worker model に移行されています。以下の設定手順に従ってください：

**手順**:

1. **local.settings.jsonを作成** (テンプレートからコピー)
   ```bash
   # API層
   cp src/api/local.settings.json.template src/api/local.settings.json
   
   # Batch層
   cp src/batch/local.settings.json.template src/batch/local.settings.json
   ```

2. **FUNCTIONS_WORKER_RUNTIMEを`dotnet-isolated`に設定**
   
   local.settings.json内で以下を確認してください：
   ```json
   "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
   ```
   
   > **重要**: .NET 10 Isolated worker modelを使用するため、`dotnet-isolated`の設定が必須です。

3. **ローカル開発: StorageConnectionStringのみ設定 (Azurite)**
   
   ローカル開発環境では、Azuriteを使用します：
   ```json
   "StorageConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;"
   ```
   
   > **注**: 上記はAzuriteの標準的な開発用接続文字列です。local.settings.json.templateから完全な接続文字列を確認してください。

4. **Azure環境: StorageAccountNameを追加してManaged Identity有効化**
   
   Azure環境では、Managed Identityを使用することを推奨します：
   - `StorageAccountName` を設定してManaged Identity認証を有効化
   - `StorageConnectionString` はフォールバック用に保持
   
   Application Settingsでの設定例：
   ```
   StorageAccountName=<your-storage-account-name>
   StorageConnectionString=DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;...
   ```

5. **AzureWebJobsStorageは接続文字列形式を継続**
   
   Durable Functions互換性のため、`AzureWebJobsStorage`は接続文字列形式を維持します：
   ```json
   "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;..."
   ```

6. **Azure Functions Core Tools v4を使用**
   
   開発には Azure Functions Core Tools v4 を使用してください：
   ```bash
   # バージョン確認
   func --version  # 4.x.x であることを確認
   
   # インストール（必要な場合）
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

#### 4. ローカル開発実行

**前提条件**:
- PostgreSQLが起動していること（Docker: `docker start comical-postgres`）
- Azurite（ローカルストレージエミュレータ）が起動していること

**実行手順**:
1. apiデバッグ実行/apiディレクトリで`func start`
2. frontディレクトリで`npm run start`
3. frontディレクトリで`npm run start:swa`
4. http://localhost:4280

## 統合テストとデプロイ

### 統合テストの実行

詳細な統合テスト手順は [統合テストガイド](./docs/INTEGRATION_TESTS.md) を参照してください。

**クイックスタート**:
```powershell
# すべてのテストを実行（ローカル環境）
cd scripts
.\test-integration.ps1 -Environment Local -RunAllTests

# API層のテストのみ実行
.\test-integration.ps1 -Environment Local -TestApi

# 開発環境でテスト実行
.\test-integration.ps1 -Environment Dev -RunAllTests
```

### 本番環境へのデプロイ

本番環境へのデプロイ手順は [デプロイチェックリスト](./docs/DEPLOYMENT_CHECKLIST.md) を参照してください。

**デプロイ前の確認事項**:
- [ ] すべての統合テストが成功している
- [ ] セキュリティスキャンが完了している
- [ ] ドキュメントが最新化されている
- [ ] Azure リソースが正しく設定されている
- [ ] コスト監視アラートが設定されている

## ドキュメント

### 主要ドキュメント

- **[Cosmos DB 移行ガイド](./docs/COSMOS_DB_MIGRATION.md)**: SQL ServerからCosmos DBへの移行手順、アーキテクチャ変更、コスト見積もり
- **[統合テストガイド](./docs/INTEGRATION_TESTS.md)**: API層、Batch層、フロントエンドの統合テスト手順
- **[デプロイチェックリスト](./docs/DEPLOYMENT_CHECKLIST.md)**: 本番環境へのデプロイ前の確認事項と手順
- **[開発計画](./docs/COSMOS_DB_MIGRATION_PLAN.md)**: Phase 1-4の開発計画とタスク依存関係

### コンポーネント別ドキュメント

- **API層**: [api/README.md](./api/README.md)
- **Batch層**: [batch/README.md](./batch/README.md)
- **フロントエンド**: [front/README.md](./front/README.md)
- **スクリプト**: [scripts/README.md](./scripts/README.md)

## コスト見積もり

**月額コスト見積もり（50,000件の漫画データ）**:

| 項目 | 月額コスト |
|------|-----------|
| PostgreSQL Flexible Server (Burstable B1ms) | $12.41 |
| PostgreSQL ストレージ (32 GiB) | $4.48 |
| Blob Storage ストレージ | $0.18 |
| Blob Storage トランザクション | $0.40 |
| Azure Functions | $0.00 (消費プラン無料枠内) |
| **合計** | **約 $17.47/月** |

> **注**: 実際のコストは使用パターンによって変動します。PostgreSQL Flexible Serverは自動スケーリングとストップ機能により、開発環境でのコストを削減できます。詳細は [Cosmos DB 移行ガイド](./docs/COSMOS_DB_MIGRATION.md#コスト見積もり) を参照してください。

## トラブルシューティング

### よくある問題

#### PostgreSQL 接続エラー
```
Npgsql.NpgsqlException: Connection refused
```
**解決方法**: 
- PostgreSQLが起動しているか確認（Docker: `docker ps | grep postgres`）
- 接続文字列が正しいか確認（ホスト、ポート、データベース名、ユーザー名）
- ファイアウォール設定を確認
- Azure環境の場合、Managed Identityの設定を確認

#### 画像が表示されない
**解決方法**:
- Blob Storage の接続文字列を確認
- `images` コンテナが存在するか確認
- CORS 設定を確認

#### Batch処理が失敗する
**解決方法**:
- 楽天APIのアプリケーションIDを確認
- レート制限（15秒待機）を確認
- ログを確認して詳細なエラーメッセージを取得

詳細なトラブルシューティング手順は [統合テストガイド](./docs/INTEGRATION_TESTS.md#トラブルシューティング) を参照してください。

## 監視とアラート

### コスト監視

Azure Cost Management で月次予算とアラートを設定することを推奨します：

```bash
# 予算の作成
az consumption budget create \
  --budget-name "ComiCal-Monthly-Budget" \
  --amount 10 \
  --time-grain Monthly \
  --resource-group ComiCal-RG
```

### パフォーマンス監視

Application Insights で以下のメトリクスを監視：
- API レスポンスタイム（目標: < 2秒）
- エラー率（目標: < 5%）
- PostgreSQL 接続プール使用率
- Blob Storage トランザクション数

詳細は [Cosmos DB 移行ガイド](./docs/COSMOS_DB_MIGRATION.md#コスト監視とアラート設定) を参照してください。

## 便利なGitコマンド

### リモートで削除されたブランチをクリーンアップ

リモートで削除されたローカルブランチを一括削除するには、以下のエイリアスを設定してください：

```bash
# エイリアスを設定（初回のみ）
git config --global alias.prune-local "!git fetch --prune && git branch -vv | grep ': gone]' | awk '{print \$1}' | xargs -r git branch -D"

# 使い方
git prune-local
```

このコマンドは：
1. `git fetch --prune` でリモートの状態を同期
2. リモートで削除されたローカルブランチを自動検出して削除

## 貢献

プルリクエストを歓迎します！以下の手順に従ってください：

1. このリポジトリをフォーク
2. フィーチャーブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

**プルリクエスト前のチェックリスト**:
- [ ] ビルドが成功する
- [ ] 統合テストが成功する
- [ ] ドキュメントを更新した
- [ ] コードレビューを受けた

## ライセンス

このプロジェクトは個人プロジェクトです。

## 連絡先

プロジェクトオーナー: [@Takas0522](https://github.com/Takas0522)

Project Link: [https://github.com/Takas0522/ComiCal](https://github.com/Takas0522/ComiCal)