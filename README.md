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
│  API Layer  │◄─────►│   Cosmos DB      │
│ (Functions) │       │  - comics        │  ← サーバーレスモード
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
- **API**: Azure Functions (.NET 6), Cosmos DB (NoSQL)
- **Batch**: Azure Durable Functions, Blob Storage
- **外部API**: 楽天ブックスAPI

**データフロー**:
1. Batch層が楽天APIから漫画データを取得
2. Cosmos DBに保存、画像はBlob Storageに保存
3. ユーザーがフロントエンドで検索
4. API層がCosmos DBからデータを取得して返却
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

スクリプト実行後、表示される接続文字列を設定ファイルに追加してください。

#### 2. Blob Storage のセットアップ

Azure Portal または Azure CLI で Blob Storage アカウントを作成し、コンテナ `images` を作成してください。

#### 3. 設定ファイルのセットアップ

テンプレートファイルをコピーして、実際の接続文字列を設定します：

**API層の設定** (`api/local.settings.json`):
```bash
# テンプレートからコピー
cp api/local.settings.json.template api/local.settings.json
```

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "CosmosConnectionString": "AccountEndpoint=https://<account-name>.documents.azure.com:443/;AccountKey=<your-key>;",
    "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=<storage-account>;AccountKey=<your-key>;EndpointSuffix=core.windows.net"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ComiCalDB;Trusted_Connection=True;"
  }
}
```

**Batch層の設定** (`batch/local.settings.json`):
```bash
# テンプレートからコピー
cp batch/local.settings.json.template batch/local.settings.json
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
| `CosmosConnectionString` | Cosmos DB 接続文字列 | `AccountEndpoint=https://...;AccountKey=...;` |
| `StorageConnectionString` | Blob Storage 接続文字列 | `DefaultEndpointsProtocol=https;AccountName=...` |
| `DefaultConnection` | SQL Server 接続文字列（オプション） | `Server=(localdb)\\mssqllocaldb;...` |
| `blobBaseUrl` | Blob Storage の画像ベースURL | `https://<account>.blob.core.windows.net/images` |

> **セキュリティ注意**: 本番環境では、接続文字列に AccountKey を使用するのではなく、Azure Managed Identity や Azure AD 認証の使用を推奨します。これにより、設定ファイルに機密情報を保存する必要がなくなります。

#### 4. ローカル開発実行

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
| Cosmos DB ストレージ | $0.025 |
| Cosmos DB RU消費 | $1.275 |
| Blob Storage ストレージ | $0.18 |
| Blob Storage トランザクション | $0.40 |
| Azure Functions | $0.00 (消費プラン無料枠内) |
| **合計** | **約 $1.88/月** |

> **注**: 実際のコストは使用パターンによって変動します。詳細は [Cosmos DB 移行ガイド](./docs/COSMOS_DB_MIGRATION.md#コスト見積もり) を参照してください。

## トラブルシューティング

### よくある問題

#### Cosmos DB 接続エラー
```
Microsoft.Azure.Cosmos.CosmosException: Unable to connect
```
**解決方法**: 
- 接続文字列が正しいか確認
- ファイアウォール設定を確認
- 詳細は [トラブルシューティングガイド](./docs/COSMOS_DB_MIGRATION.md#トラブルシューティング)

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
- Cosmos DB RU消費量
- Blob Storage トランザクション数

詳細は [Cosmos DB 移行ガイド](./docs/COSMOS_DB_MIGRATION.md#コスト監視とアラート設定) を参照してください。

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