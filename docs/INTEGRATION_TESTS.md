# Integration Tests Guide

このドキュメントでは、ComiCal システムの統合テスト手順を説明します。

## 概要

統合テストでは、システム全体が正しく連携して動作することを確認します：
- API層: Cosmos DB からのデータ取得
- Batch層: 楽天API → Cosmos DB → Blob Storage のデータフロー
- フロントエンド: 検索、画像表示、エラーハンドリング

## 前提条件

### 必要な環境
- Azure Cosmos DB アカウント（開発環境またはエミュレータ）
- Azure Blob Storage アカウント
- Azure Functions Core Tools (v4.x)
- Node.js 18.x
- .NET 6.0 SDK
- 楽天 Books API アプリケーションID

### 環境設定

#### 1. Cosmos DB Emulator（ローカル開発の場合）

Cosmos DB Emulator のダウンロードとインストール:
```powershell
# Windows の場合
# https://aka.ms/cosmosdb-emulator からダウンロード

# 起動確認
# https://localhost:8081/_explorer/index.html にアクセス
```

エミュレータ接続文字列:
```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
```

#### 2. 設定ファイルの準備

**API層** (`api/local.settings.json`):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "CosmosConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=...",
    "StorageConnectionString": "UseDevelopmentStorage=true"
  }
}
```

**Batch層** (`batch/local.settings.json`):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "CosmosConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=...",
    "StorageConnectionString": "UseDevelopmentStorage=true",
    "RakutenApplicationId": "<your-application-id>"
  }
}
```

#### 3. Cosmos DB コンテナの初期化

```powershell
cd scripts
.\setup-cosmosdb.ps1 -CosmosAccountName "<account-name>" -ResourceGroupName "<resource-group>"
```

エミュレータの場合:
```powershell
# Azure Cosmos DB Emulator が起動している状態で
.\setup-cosmosdb.ps1 -UseEmulator
```

#### 4. Azure Storage Emulator（ローカル開発の場合）

```powershell
# Azurite のインストールと起動
npm install -g azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

## API層の統合テスト

### テスト1: GetComics API - 基本的な検索

#### 目的
キーワード検索が正常に動作し、Cosmos DB からデータを取得できることを確認

#### 手順

1. **API を起動**
```powershell
cd api
func start
```

2. **テストデータを Cosmos DB に登録**（初回のみ）
```powershell
# Batch を使用してテストデータを登録するか、
# Azure Portal の Data Explorer から手動で登録
```

サンプルテストデータ:
```json
{
  "id": "9784088820000",
  "type": "comic",
  "title": "テストマンガ 1巻",
  "author": "テスト作者",
  "publisherName": "テスト出版",
  "salesDate": "2024-01-15",
  "itemCaption": "テストマンガの説明",
  "largeImageUrl": "https://example.com/image.jpg",
  "affiliateUrl": "https://example.com/affiliate"
}
```

3. **API にリクエストを送信**
```powershell
# PowerShell
$body = @{
    SearchList = @("テストマンガ")
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/ComicData?fromdate=2024-01-01" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

```bash
# Bash/curl
curl -X POST "http://localhost:7071/api/ComicData?fromdate=2024-01-01" \
  -H "Content-Type: application/json" \
  -d '{"SearchList":["テストマンガ"]}'
```

#### 期待される結果
- ステータスコード: 200 OK
- レスポンスに検索条件に一致する漫画データが含まれる
- 各エントリに `id`, `title`, `author`, `salesDate` などのプロパティが含まれる
- 画像URL が正しく含まれる（または画像がない場合は null/空）

#### 検証項目
- [ ] API が正常に起動する
- [ ] リクエストが成功する（200 OK）
- [ ] 検索キーワードに一致するデータが返される
- [ ] データ構造が正しい
- [ ] 日付フィルタ（fromdate）が正しく機能する

### テスト2: GetComics API - 複数キーワード検索

#### 手順
```powershell
$body = @{
    SearchList = @("ワンピース", "ナルト", "ブリーチ")
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/ComicData?fromdate=2024-01-01" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

#### 期待される結果
- すべてのキーワードに一致する漫画が返される
- OR 条件で検索される（いずれかのキーワードに一致）

### テスト3: ConfigMigration API - 設定の保存と取得

#### 目的
設定移行機能が正常に動作することを確認

#### 手順

1. **設定を保存**
```powershell
$body = @("keyword1", "keyword2", "keyword3") | ConvertTo-Json

$result = Invoke-RestMethod -Uri "http://localhost:7071/api/ConfigMigration" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"

$migrationId = $result.Id
Write-Host "Migration ID: $migrationId"
```

2. **設定を取得**
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/ConfigMigration?id=$migrationId" `
    -Method GET
```

#### 期待される結果
- POST: 新しい Migration ID が返される
- GET: 保存したキーワードリストが正しく返される

#### 検証項目
- [ ] POST で設定を保存できる
- [ ] 一意の ID が生成される
- [ ] GET で保存した設定を取得できる
- [ ] データの整合性が保たれる

## Batch層の統合テスト

### テスト4: Batch処理 - 楽天API → Cosmos DB → Blob Storage

#### 目的
バッチ処理全体のデータフローが正常に動作することを確認

#### 手順

1. **Batch を起動**
```powershell
cd batch
func start
```

2. **手動でオーケストレーションを開始**（デバッグ時）
```powershell
# TimerStart 関数のデバッグフラグが有効な場合、起動時に自動実行される
# または、Durable Functions の管理API を使用して手動起動
```

3. **ログを監視**
```
Functions runtime is ready
Executing 'TimerStart'
Started orchestration with ID = '...'
Get PageCount Result=...
Run Page: 1
Data Get Complete
```

#### 期待される動作

1. **楽天APIからデータ取得**
   - ページカウントを取得
   - 各ページのデータを順次取得
   - API レート制限を考慮（15秒待機）

2. **Cosmos DB へ登録**
   - 漫画データを `comics` コンテナに保存
   - ISBN (id) をパーティションキーとして使用
   - 重複する場合は上書き（Upsert）

3. **Blob Storage へ画像保存**
   - 画像URLから画像をダウンロード
   - Content-Type を判定（JPEG/PNG/GIF/WebP）
   - `images/{isbn}.{ext}` 形式で保存
   - Blob のメタデータに Content-Type を設定

#### 検証項目
- [ ] 楽天APIからデータを取得できる
- [ ] Cosmos DB にデータが保存される
- [ ] Blob Storage に画像が保存される
- [ ] 適切な Content-Type が設定される
- [ ] エラーハンドリングが正常に機能する
- [ ] ログ出力が適切

### テスト5: 画像のダウンロードと保存

#### 手順

1. **Blob Storage を確認**
```powershell
# Azure Storage Explorer を使用するか、Azure Portal で確認
# コンテナ: images
# ファイル形式: {isbn}.jpg, {isbn}.png など
```

2. **画像が存在することを確認**
```powershell
# Azure CLI を使用
az storage blob list --container-name images --account-name <storage-account> --output table
```

#### 期待される結果
- 各漫画の ISBN に対応する画像ファイルが存在する
- Content-Type が正しく設定されている（image/jpeg, image/png など）
- 画像がダウンロード可能

## フロントエンドの動作確認

### テスト6: 検索機能のテスト

#### 手順

1. **フロントエンドを起動**
```powershell
cd front
npm install
npm run start
```

2. **ブラウザでアクセス**
```
http://localhost:4200
```

3. **検索を実行**
   - 検索キーワードを入力（例: "ワンピース"）
   - 検索ボタンをクリック

#### 期待される動作
- 検索キーワードが API に送信される
- 検索結果が表示される
- 各漫画のタイトル、著者、発売日が表示される

#### 検証項目
- [ ] 検索フォームが正常に動作する
- [ ] 検索結果が表示される
- [ ] ローディング状態が適切に表示される
- [ ] エラーハンドリングが機能する

### テスト7: 画像表示のテスト

#### 手順

1. **画像が存在する漫画を検索**
2. **結果一覧で画像が表示されることを確認**

#### 期待される動作
- 画像が正しく表示される
- 画像の遅延読み込み（Lazy Loading）が機能する
- 画像読み込み中にプレースホルダーが表示される

#### 検証項目
- [ ] 画像が正しく表示される
- [ ] Blob Storage からの画像取得が成功する
- [ ] 画像がキャッシュされる

### テスト8: 画像なし表示のテスト

#### 手順

1. **画像が存在しない漫画を検索（または画像URLを意図的に無効化）**
2. **404エラー時に "画像なし" が表示されることを確認**

#### 期待される動作
- 画像読み込みが失敗した場合、デフォルト画像または "画像なし" テキストが表示される
- エラーが UI に影響を与えない（アプリがクラッシュしない）

#### 検証項目
- [ ] 画像404時にフォールバックが機能する
- [ ] ユーザーに適切なメッセージが表示される
- [ ] エラーがログに記録される

## E2Eテストシナリオ

### シナリオ1: 新規漫画の登録から表示まで

1. **Batch処理を実行** → 楽天APIから最新データを取得
2. **Cosmos DB を確認** → 新しいデータが登録されている
3. **Blob Storage を確認** → 画像が保存されている
4. **フロントエンドで検索** → 新しい漫画が検索結果に表示される
5. **画像をクリック** → 画像が正しく表示される

### シナリオ2: 設定移行機能の動作確認

1. **フロントエンドで検索キーワードを設定**
2. **"コードを生成" ボタンをクリック** → Migration ID が生成される
3. **別のブラウザで Migration ID を入力** → 検索キーワードが復元される
4. **検索を実行** → 同じ検索結果が表示される

## トラブルシューティング

### Cosmos DB 接続エラー

**症状**: `Unable to connect to Cosmos DB`

**解決方法**:
- 接続文字列が正しいか確認
- Cosmos DB アカウント/エミュレータが起動しているか確認
- ファイアウォール設定を確認
- ネットワーク接続を確認

### Blob Storage 接続エラー

**症状**: `Blob Storage container not found`

**解決方法**:
- Storage アカウントに `images` コンテナが存在するか確認
- 接続文字列が正しいか確認
- Azurite が起動しているか確認（ローカル開発の場合）

### 楽天API エラー

**症状**: `Rakuten API returned error`

**解決方法**:
- Application ID が正しいか確認
- APIクォータを確認（1秒あたりのリクエスト数制限）
- ネットワーク接続を確認

### フロントエンド CORS エラー

**症状**: `CORS policy: No 'Access-Control-Allow-Origin' header`

**解決方法**:
- API の CORS 設定を確認（host.json）
- ローカル開発では SWA CLI を使用 (`npm run start:swa`)

## 自動化テストスクリプト

統合テストを自動化するには、`scripts/test-integration.ps1` スクリプトを使用します：

```powershell
cd scripts
.\test-integration.ps1 -Environment Local -RunAllTests
```

オプション:
- `-Environment Local|Dev|Prod`: テスト環境
- `-RunAllTests`: すべてのテストを実行
- `-TestApi`: API テストのみ実行
- `-TestBatch`: Batch テストのみ実行
- `-TestFrontend`: フロントエンドテストのみ実行

## 継続的インテグレーション

GitHub Actions を使用した自動テスト:
- `.github/workflows/integration-tests.yml` を参照
- プルリクエスト作成時に自動実行
- マージ前に統合テストが成功することを確認

## 次のステップ

- [本番環境へのデプロイ](./DEPLOYMENT_CHECKLIST.md)
- [コスト監視の設定](./COSMOS_DB_MIGRATION.md#cost-monitoring)
- [トラブルシューティングガイド](./COSMOS_DB_MIGRATION.md#troubleshooting)
