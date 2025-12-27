# Cosmos DB Migration Guide

このドキュメントでは、ComiCal システムを SQL Server から Azure Cosmos DB に移行するための包括的なガイドを提供します。

## 目次

- [概要](#概要)
- [アーキテクチャの変更](#アーキテクチャの変更)
- [移行の利点](#移行の利点)
- [コスト見積もり](#コスト見積もり)
- [移行手順](#移行手順)
- [データモデルの変更](#データモデルの変更)
- [コスト監視とアラート設定](#コスト監視とアラート設定)
- [トラブルシューティング](#トラブルシューティング)
- [ロールバック手順](#ロールバック手順)

## 概要

ComiCal システムは、漫画情報を管理し、ユーザーに発売日のリマインダーを提供するアプリケーションです。この移行では、データベースを従来の SQL Server から Azure Cosmos DB（サーバーレスモード）に移行します。

### 移行の背景

- **スケーラビリティ**: グローバルな分散と自動スケーリング
- **パフォーマンス**: 低レイテンシーの読み書き操作
- **コスト最適化**: サーバーレスモードによる使用量ベースの課金
- **開発効率**: NoSQL の柔軟なスキーマ

## アーキテクチャの変更

### Before: SQL Server ベースのアーキテクチャ

```
┌─────────────┐
│  Frontend   │
│  (Angular)  │
└──────┬──────┘
       │ HTTP
       ▼
┌─────────────┐       ┌──────────────┐
│  API Layer  │◄─────►│  SQL Server  │
│ (Functions) │       │  - Comic     │
└──────┬──────┘       │  - ComicImage│
       │              └──────────────┘
       │
┌──────▼──────┐       ┌──────────────┐
│ Batch Layer │◄─────►│ Rakuten API  │
│ (Functions) │       └──────────────┘
└─────────────┘
       │
       ▼
┌─────────────┐
│    Local    │
│   Storage   │
└─────────────┘
```

**課題**:
- テーブル結合のオーバーヘッド (Comic + ComicImage)
- 画像データをデータベースに保存（Base64）
- スケーリングの制約
- 運用コストが高い

### After: Cosmos DB + Blob Storage アーキテクチャ

```
┌─────────────┐
│  Frontend   │
│  (Angular)  │
└──────┬──────┘
       │ HTTP
       ▼
┌─────────────┐       ┌──────────────────┐
│  API Layer  │◄─────►│   Cosmos DB      │
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
┌─────────────┐       ┌──────────────┐
│Blob Storage │       │  CDN (将来)  │
│  - images   │◄─────►│  (Optional)  │
└─────────────┘       └──────────────┘
```

**改善点**:
- 非正規化されたドキュメントモデル（結合不要）
- 画像は Blob Storage で管理
- 自動スケーリングとグローバル配布
- サーバーレスモードによるコスト最適化
- CDN 統合の準備（将来的な拡張）

## 移行の利点

### 1. パフォーマンス向上

- **結合の削除**: Comic と ComicImage を統合し、単一ドキュメントクエリに
- **低レイテンシー**: Cosmos DB の SSD ベースストレージ
- **インデックス最適化**: カスタムインデックスポリシーによるクエリ最適化

### 2. スケーラビリティ

- **自動スケーリング**: サーバーレスモードで需要に応じて自動調整
- **グローバル配布**: 複数リージョンへの展開が容易（将来的な拡張）
- **無制限のストレージ**: コンテナあたり無制限のドキュメント保存

### 3. 開発効率

- **スキーマレス**: データ構造の柔軟な変更
- **JSON ネイティブ**: .NET オブジェクトとの直接マッピング
- **統合SDK**: Azure Functions との緊密な統合

### 4. コスト最適化

- **従量課金**: 使用した分だけ課金（サーバーレスモード）
- **ストレージコスト**: $0.25/GB と競争力のある価格
- **管理コスト削減**: マネージドサービスによる運用負荷軽減

## コスト見積もり

### 前提条件

- **データ量**: 約50,000件の漫画データ
- **ドキュメントサイズ**: 平均2KB/ドキュメント
- **月間読み取り操作**: 1,000,000回
- **月間書き込み操作**: 10,000回
- **ストレージ**: 100MB（ドキュメントのみ、画像は別途Blob Storage）

### Cosmos DB サーバーレスモードのコスト

#### ストレージコスト
```
100MB ÷ 1024 = 0.098GB
0.098GB × $0.25 = $0.025/月
```

#### Request Units (RU) コスト

**読み取り操作**:
- 単一ドキュメント読み取り: 約5 RU
- クエリ（10件取得）: 約50 RU
- 月間読み取りRU消費: 1,000,000 × 5 = 5,000,000 RU

**書き込み操作**:
- 単一ドキュメント作成/更新: 約10 RU
- 月間書き込みRU消費: 10,000 × 10 = 100,000 RU

**合計RU消費**:
```
5,000,000 + 100,000 = 5,100,000 RU/月
5,100,000 RU ÷ 1,000,000 = 5.1M RU
5.1M RU × $0.25 = $1.275/月
```

#### Blob Storage コスト（画像保存）

- **ストレージ**: 10GB（平均200KB × 50,000件）
- **ストレージコスト**: 10GB × $0.018 = $0.18/月
- **トランザクションコスト**: 読み取り 1,000,000回 × $0.004/10,000 = $0.40/月

### 総コスト見積もり

| 項目 | 月額コスト |
|------|-----------|
| Cosmos DB ストレージ | $0.025 |
| Cosmos DB RU消費 | $1.275 |
| Blob Storage ストレージ | $0.18 |
| Blob Storage トランザクション | $0.40 |
| **合計** | **$1.88/月** |

> **注**: 実際のコストは使用パターンによって変動します。Azure Cost Management でリアルタイムのコスト監視を推奨します。

### SQL Server との比較

| 項目 | SQL Server | Cosmos DB + Blob |
|------|-----------|------------------|
| 月額コスト | $50-100 | $1.88 |
| スケーラビリティ | 制限あり | 自動スケーリング |
| グローバル配布 | 複雑 | 容易 |
| バックアップ | 手動設定 | 自動 |
| 運用負荷 | 高 | 低 |

**節約額**: 約 $48-98/月（96-98% のコスト削減）

## 移行手順

### Phase 1: 準備（1-2日）

#### 1.1 Azure リソースの作成

```bash
# Azure CLI でログイン
az login

# リソースグループの作成（既存の場合はスキップ）
az group create --name ComiCal-RG --location japaneast

# Cosmos DB アカウントの作成（サーバーレスモード）
az cosmosdb create \
  --name comical-cosmos-<unique-id> \
  --resource-group ComiCal-RG \
  --capabilities EnableServerless \
  --locations regionName=japaneast

# Blob Storage アカウントの作成
az storage account create \
  --name comicalstorage<unique-id> \
  --resource-group ComiCal-RG \
  --location japaneast \
  --sku Standard_LRS \
  --kind StorageV2
```

#### 1.2 コンテナとデータベースの初期化

```powershell
cd scripts
.\setup-cosmosdb.ps1 -CosmosAccountName "comical-cosmos-<unique-id>" -ResourceGroupName "ComiCal-RG"
```

スクリプトは以下を自動作成:
- データベース: `ComiCalDB`
- コンテナ: `comics` (パーティションキー: `/id`)
- コンテナ: `config-migrations` (パーティションキー: `/id`)
- 最適化されたインデックスポリシー

#### 1.3 Blob Storage コンテナの作成

```bash
# images コンテナの作成
az storage container create \
  --name images \
  --account-name comicalstorage<unique-id> \
  --public-access blob
```

### Phase 2: データ移行（1-3日）

#### 2.1 既存データのエクスポート

**SQL Server からデータをエクスポート**:

```sql
-- Comic と ComicImage を結合してエクスポート
SELECT 
    c.ISBN as id,
    'comic' as type,
    c.Title as title,
    c.Author as author,
    c.PublisherName as publisherName,
    c.SalesDate as salesDate,
    c.ItemCaption as itemCaption,
    c.LargeImageUrl as largeImageUrl,
    c.AffiliateUrl as affiliateUrl,
    ci.ImageData as imageData
FROM Comic c
LEFT JOIN ComicImage ci ON c.ISBN = ci.ISBN
ORDER BY c.SalesDate DESC;
```

エクスポート先: `migration-data.json`

#### 2.2 データ形式の変換

PowerShell スクリプト例:
```powershell
$sqlData = Import-Csv "migration-data.csv"
$cosmosData = @()

foreach ($row in $sqlData) {
    $doc = @{
        id = $row.id
        type = "comic"
        title = $row.title
        author = $row.author
        publisherName = $row.publisherName
        salesDate = $row.salesDate
        itemCaption = $row.itemCaption
        largeImageUrl = $row.largeImageUrl
        affiliateUrl = $row.affiliateUrl
    }
    $cosmosData += $doc
}

$cosmosData | ConvertTo-Json -Depth 10 | Out-File "cosmos-data.json"
```

#### 2.3 Cosmos DB へデータをインポート

```bash
# Azure Data Migration Tool を使用
dt.exe /s:JsonFile /s.Files:"cosmos-data.json" \
  /t:CosmosDB \
  /t.ConnectionString:"AccountEndpoint=https://...;AccountKey=...;" \
  /t.Database:ComiCalDB \
  /t.Collection:comics \
  /t.PartitionKey:/id
```

または、Azure Portal の Data Explorer から手動インポート。

#### 2.4 画像データの移行

```powershell
# SQL Server から画像を抽出して Blob Storage にアップロード
$images = Invoke-Sqlcmd -Query "SELECT ISBN, ImageData FROM ComicImage"

foreach ($img in $images) {
    $isbn = $img.ISBN
    $imageBytes = [Convert]::FromBase64String($img.ImageData)
    
    # 画像形式を判定
    $ext = Get-ImageExtension -Bytes $imageBytes
    
    # Blob Storage にアップロード
    $blobName = "$isbn.$ext"
    az storage blob upload \
      --container-name images \
      --file temp-image.$ext \
      --name $blobName \
      --account-name comicalstorage<unique-id>
}
```

### Phase 3: アプリケーション更新（完了済み）

以下は Phase 1-4 で実装済み:

- ✅ データモデルの更新（Comic と ComicImage の統合）
- ✅ Cosmos DB 接続プロバイダー実装
- ✅ Repository 層の Cosmos DB 実装
- ✅ Service 層の更新
- ✅ フロントエンドの画像URL動的生成
- ✅ 設定ファイルとDI登録

### Phase 4: テストとデプロイ（2-3日）

#### 4.1 統合テストの実施

詳細は [統合テストガイド](./INTEGRATION_TESTS.md) を参照。

**チェックリスト**:
- [ ] API層テスト（GetComics, ConfigMigration）
- [ ] Batch層テスト（Rakuten API → Cosmos DB → Blob Storage）
- [ ] フロントエンドテスト（検索、画像表示、404処理）
- [ ] パフォーマンステスト（レスポンスタイム測定）
- [ ] 負荷テスト（同時アクセス数）

#### 4.2 段階的デプロイ

**ステップ1: 開発環境**
```bash
# 開発環境にデプロイ
func azure functionapp publish comical-api-dev
func azure functionapp publish comical-batch-dev
```

**ステップ2: ステージング環境**
```bash
# ステージング環境にデプロイ
func azure functionapp publish comical-api-staging
func azure functionapp publish comical-batch-staging
```

**ステップ3: 本番環境**
```bash
# デプロイスロットを使用したブルーグリーンデプロイ
az functionapp deployment slot create \
  --name comical-api-prod \
  --resource-group ComiCal-RG \
  --slot staging

# ステージングスロットにデプロイ
func azure functionapp publish comical-api-prod --slot staging

# 動作確認後、スワップ
az functionapp deployment slot swap \
  --name comical-api-prod \
  --resource-group ComiCal-RG \
  --slot staging
```

#### 4.3 本番環境の設定

**Application Settings**:
```json
{
  "CosmosConnectionString": "AccountEndpoint=https://comical-cosmos-prod.documents.azure.com:443/;AccountKey=...",
  "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=comicalstorageprodd;...",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet",
  "WEBSITE_RUN_FROM_PACKAGE": "1"
}
```

> **セキュリティのベストプラクティス**: 本番環境では Azure Key Vault を使用して接続文字列を管理することを推奨します。

## データモデルの変更

### 旧モデル（SQL Server）

**Comic テーブル**:
```sql
CREATE TABLE Comic (
    ISBN VARCHAR(50) PRIMARY KEY,
    Title NVARCHAR(200),
    Author NVARCHAR(100),
    PublisherName NVARCHAR(100),
    SalesDate DATE,
    ItemCaption NVARCHAR(MAX),
    LargeImageUrl VARCHAR(500),
    AffiliateUrl VARCHAR(500)
);
```

**ComicImage テーブル**:
```sql
CREATE TABLE ComicImage (
    ISBN VARCHAR(50) PRIMARY KEY,
    ImageData NVARCHAR(MAX), -- Base64
    FOREIGN KEY (ISBN) REFERENCES Comic(ISBN)
);
```

### 新モデル（Cosmos DB）

**comics コンテナ**:
```json
{
  "id": "9784088820000",
  "type": "comic",
  "title": "ワンピース 100巻",
  "author": "尾田栄一郎",
  "publisherName": "集英社",
  "salesDate": "2024-01-15T00:00:00Z",
  "itemCaption": "描き下ろし特別版",
  "largeImageUrl": "https://comicalstorage.blob.core.windows.net/images/9784088820000.jpg",
  "affiliateUrl": "https://..."
}
```

**変更点**:
- `ISBN` → `id` (パーティションキー)
- `type` フィールド追加（将来的な拡張のため）
- `ImageData` 削除（Blob Storage で管理）
- `largeImageUrl` は Blob Storage の URL を動的生成

**config-migrations コンテナ**:
```json
{
  "id": "a1b2c3d4-...",
  "data": ["keyword1", "keyword2", "keyword3"],
  "_ts": 1640000000
}
```

### インデックスポリシー

**comics コンテナ**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    { "path": "/title/?" },
    { "path": "/author/?" },
    { "path": "/publisherName/?" },
    { "path": "/salesDate/?" }
  ],
  "excludedPaths": [
    { "path": "/itemCaption/*" },
    { "path": "/affiliateUrl/*" }
  ]
}
```

**最適化のポイント**:
- 検索に使用するフィールドのみインデックス化
- 大きなテキストフィールド（itemCaption）は除外
- ストレージとRUコストを削減

## コスト監視とアラート設定

### Azure Cost Management の設定

#### 1. 予算の作成

```bash
# Azure CLI で予算を作成
az consumption budget create \
  --budget-name "ComiCal-Monthly-Budget" \
  --amount 10 \
  --time-grain Monthly \
  --start-date 2024-01-01 \
  --end-date 2025-12-31 \
  --resource-group ComiCal-RG
```

#### 2. コストアラートの設定

**Azure Portal での設定手順**:

1. Azure Portal → Cost Management → Budgets
2. "Add" をクリック
3. 予算設定:
   - Name: `ComiCal-Monthly-Budget`
   - Amount: $10
   - Alert conditions:
     - 50% ($5): メール通知
     - 75% ($7.5): メール通知
     - 90% ($9): メール + SMS通知
     - 100% ($10): 緊急アラート

#### 3. Azure Monitor の設定

**Cosmos DB メトリクス監視**:

```bash
# RU消費量のアラート設定
az monitor metrics alert create \
  --name "High-RU-Consumption" \
  --resource-group ComiCal-RG \
  --scopes "/subscriptions/{sub-id}/resourceGroups/ComiCal-RG/providers/Microsoft.DocumentDB/databaseAccounts/comical-cosmos" \
  --condition "avg TotalRequestUnits > 1000000" \
  --window-size 1h \
  --evaluation-frequency 5m \
  --action-group alert-group
```

**監視すべきメトリクス**:
- Total Request Units
- Total Requests
- Data Usage
- Availability
- Server-side latency

### コストダッシュボード

**Azure Workbook でカスタムダッシュボードを作成**:

1. Azure Portal → Monitor → Workbooks
2. 新しいワークブックを作成
3. 以下のクエリを追加:

```kusto
// 日別のコスト推移
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.DOCUMENTDB"
| summarize TotalRU = sum(RequestCharge) by bin(TimeGenerated, 1d)
| project TimeGenerated, DailyCost = TotalRU / 1000000 * 0.25
```

### コスト最適化の推奨事項

1. **クエリの最適化**
   - WHERE 句でパーティションキーを使用
   - SELECT で必要なフィールドのみ取得
   - ページングを適切に実装

2. **インデックスの見直し**
   - 使用しないフィールドのインデックスを削除
   - 複合インデックスの活用

3. **TTL（Time-To-Live）の設定**
   - 古いデータの自動削除を検討
   - 履歴データのアーカイブ

4. **キャッシュの活用**
   - Azure Redis Cache の導入検討
   - クライアントサイドキャッシュの実装

## トラブルシューティング

### 問題1: Cosmos DB 接続エラー

**症状**:
```
Microsoft.Azure.Cosmos.CosmosException: Unable to connect to Cosmos DB
```

**原因**:
- 接続文字列が間違っている
- ファイアウォール設定で接続がブロックされている
- ネットワークの問題

**解決方法**:
```bash
# 1. 接続文字列の確認
az cosmosdb keys list --name comical-cosmos --resource-group ComiCal-RG

# 2. ファイアウォール設定の確認
az cosmosdb network-rule list --name comical-cosmos --resource-group ComiCal-RG

# 3. Functions App に IP アドレスを追加
az cosmosdb network-rule add \
  --name comical-cosmos \
  --resource-group ComiCal-RG \
  --ip-address <functions-app-ip>
```

### 問題2: RU消費量が予想より高い

**症状**:
- 予算を超過するアラート
- クエリが遅い

**診断手順**:

1. **診断ログの有効化**
```bash
az monitor diagnostic-settings create \
  --name cosmos-diagnostics \
  --resource /subscriptions/{sub-id}/resourceGroups/ComiCal-RG/providers/Microsoft.DocumentDB/databaseAccounts/comical-cosmos \
  --logs '[{"category": "DataPlaneRequests", "enabled": true}]' \
  --workspace /subscriptions/{sub-id}/resourceGroups/ComiCal-RG/providers/Microsoft.OperationalInsights/workspaces/comical-logs
```

2. **高コストクエリの特定**
```kusto
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.DOCUMENTDB"
| where Category == "DataPlaneRequests"
| summarize TotalRU = sum(RequestCharge), Count = count() by OperationName
| order by TotalRU desc
```

3. **クエリの最適化**
   - パーティションキーを使用
   - インデックスを見直し
   - ページサイズを調整

### 問題3: Blob Storage からの画像読み込みが遅い

**症状**:
- 画像表示に時間がかかる
- タイムアウトエラー

**解決方法**:

1. **CDN の導入**
```bash
# Azure CDN プロファイルの作成
az cdn profile create \
  --name comical-cdn \
  --resource-group ComiCal-RG \
  --sku Standard_Microsoft

# CDN エンドポイントの作成
az cdn endpoint create \
  --name comical-images \
  --profile-name comical-cdn \
  --resource-group ComiCal-RG \
  --origin comicalstorage.blob.core.windows.net
```

2. **キャッシュポリシーの設定**
   - Browser cache: 7 days
   - CDN cache: 30 days

3. **画像の最適化**
   - WebP 形式への変換
   - サムネイルの生成
   - 遅延読み込みの実装

### 問題4: Batch処理の失敗

**症状**:
```
Activity function 'Register' failed: System.Net.Http.HttpRequestException
```

**原因**:
- 楽天APIのレート制限
- ネットワークタイムアウト
- Cosmos DB への書き込みエラー

**解決方法**:

1. **リトライポリシーの実装**
```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

await retryPolicy.ExecuteAsync(async () => 
{
    await _rakutenRepository.Fetch(page);
});
```

2. **待機時間の調整**
   - 楽天APIの待機時間を15秒から30秒に増加
   - Durable Functions のタイムアウト設定を確認

3. **エラーハンドリングの追加**
   - 失敗したページを記録
   - 手動再試行メカニズムの実装

## ロールバック手順

万が一、移行に問題が発生した場合のロールバック手順:

### ステップ1: トラフィックを旧環境に戻す

```bash
# デプロイスロットをスワップして元に戻す
az functionapp deployment slot swap \
  --name comical-api-prod \
  --resource-group ComiCal-RG \
  --slot staging \
  --target-slot production
```

### ステップ2: 設定を旧接続文字列に戻す

```bash
# SQL Server 接続文字列に戻す
az functionapp config appsettings set \
  --name comical-api-prod \
  --resource-group ComiCal-RG \
  --settings ConnectionStrings__DefaultConnection="Server=...;Database=ComiCalDB;..."
```

### ステップ3: データの整合性確認

- 最新のバックアップから SQL Server を復元
- 移行中のデータ変更を確認
- 必要に応じて手動でデータを同期

### ステップ4: Cosmos DB からデータをエクスポート（必要な場合）

```bash
# Data Migration Tool でエクスポート
dt.exe /s:CosmosDB \
  /s.ConnectionString:"AccountEndpoint=...;AccountKey=...;" \
  /s.Database:ComiCalDB \
  /s.Collection:comics \
  /t:JsonFile /t.File:rollback-data.json
```

## ベストプラクティス

### 開発

1. **ローカル開発には Cosmos DB Emulator を使用**
   - コスト削減
   - オフライン開発が可能

2. **パーティションキーの適切な選択**
   - 均等な分散
   - クエリパターンに基づく設計

3. **リトライロジックの実装**
   - 429 (Too Many Requests) への対応
   - 指数バックオフの使用

### 運用

1. **定期的なバックアップ**
   - Cosmos DB の自動バックアップを有効化
   - 重要なデータは追加の手動バックアップ

2. **モニタリングとアラート**
   - コスト、パフォーマンス、エラー率を監視
   - 異常検知の自動化

3. **ドキュメント管理**
   - データモデルの変更履歴を記録
   - 運用手順書の更新

### セキュリティ

1. **Azure Key Vault の使用**
   - 接続文字列を Key Vault に保存
   - Managed Identity でアクセス

2. **ファイアウォール設定**
   - IP制限を有効化
   - Virtual Network 統合を検討

3. **アクセス制御**
   - RBAC で適切な権限管理
   - 最小権限の原則を適用

## 次のステップ

- [統合テストの実施](./INTEGRATION_TESTS.md)
- [デプロイチェックリスト](./DEPLOYMENT_CHECKLIST.md)
- [パフォーマンスチューニングガイド](./PERFORMANCE_TUNING.md)（将来追加予定）

## 参考資料

- [Azure Cosmos DB ドキュメント](https://learn.microsoft.com/ja-jp/azure/cosmos-db/)
- [Cosmos DB パーティション設計](https://learn.microsoft.com/ja-jp/azure/cosmos-db/partitioning-overview)
- [Cosmos DB コスト管理](https://learn.microsoft.com/ja-jp/azure/cosmos-db/plan-manage-costs)
- [Azure Blob Storage ドキュメント](https://learn.microsoft.com/ja-jp/azure/storage/blobs/)
