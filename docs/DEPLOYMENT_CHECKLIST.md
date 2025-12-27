# Deployment Checklist

本番環境へのデプロイ前に確認すべき項目のチェックリストです。

## 📋 デプロイ前チェックリスト

### 1. コードとテスト

- [ ] すべてのコードがリポジトリにコミットされている
- [ ] すべてのマージコンフリクトが解決されている
- [ ] ローカルでビルドが成功する
  ```bash
  cd api && dotnet build
  cd batch && dotnet build
  cd front && npm run build
  ```
- [ ] ユニットテストが通過する
  ```bash
  cd front && npm test
  ```
- [ ] 統合テストが成功する（[統合テストガイド](./INTEGRATION_TESTS.md)参照）
- [ ] コードレビューが完了している
- [ ] セキュリティスキャンが完了している（既知の脆弱性なし）

### 2. Azure リソース

#### Cosmos DB
- [ ] Cosmos DB アカウントが作成されている（サーバーレスモード）
- [ ] データベース `ComiCalDB` が作成されている
- [ ] コンテナ `comics` が作成されている（パーティションキー: `/id`）
- [ ] コンテナ `config-migrations` が作成されている（パーティションキー: `/id`）
- [ ] インデックスポリシーが設定されている
- [ ] ファイアウォール設定が適切に構成されている
- [ ] 自動バックアップが有効になっている
- [ ] 診断ログが有効になっている

#### Blob Storage
- [ ] Blob Storage アカウントが作成されている
- [ ] コンテナ `images` が作成されている
- [ ] パブリックアクセスレベルが適切に設定されている（Blob）
- [ ] CORS 設定が構成されている
- [ ] ライフサイクル管理ポリシーが設定されている（オプション）

#### Azure Functions
- [ ] API Functions App が作成されている（消費プラン推奨）
- [ ] Batch Functions App が作成されている（消費プラン推奨）
- [ ] Application Insights が有効になっている
- [ ] デプロイスロット（staging）が作成されている
- [ ] システム割り当てマネージド ID が有効になっている（オプション）

#### Azure Static Web Apps
- [ ] Static Web App が作成されている
- [ ] カスタムドメインが設定されている（オプション）
- [ ] SSL証明書が有効になっている

### 3. 設定とシークレット

#### API Functions App Settings
- [ ] `CosmosConnectionString` が設定されている
- [ ] `StorageConnectionString` が設定されている
- [ ] `FUNCTIONS_WORKER_RUNTIME` = "dotnet"
- [ ] `WEBSITE_RUN_FROM_PACKAGE` = "1"
- [ ] `APPINSIGHTS_INSTRUMENTATIONKEY` が設定されている

#### Batch Functions App Settings
- [ ] `CosmosConnectionString` が設定されている
- [ ] `StorageConnectionString` が設定されている
- [ ] `RakutenApplicationId` が設定されている
- [ ] `FUNCTIONS_WORKER_RUNTIME` = "dotnet"
- [ ] `WEBSITE_RUN_FROM_PACKAGE` = "1"
- [ ] `APPINSIGHTS_INSTRUMENTATIONKEY` が設定されている

#### Static Web App Configuration
- [ ] API エンドポイントが正しく設定されている
- [ ] 環境変数 `blobBaseUrl` が設定されている
- [ ] Google Calendar API クライアントIDが設定されている（該当する場合）

### 4. セキュリティ

- [ ] すべての接続文字列が Azure Key Vault に保存されている（推奨）
- [ ] シークレットがソースコードにハードコードされていない
- [ ] `.gitignore` に機密情報ファイルが含まれている
  - `local.settings.json`
  - `*.key`
  - `appsettings.*.json` （ローカル用）
- [ ] CORS 設定が適切に構成されている
- [ ] Function App の認証レベルが適切に設定されている
- [ ] ファイアウォールとネットワークセキュリティグループが設定されている
- [ ] 最小権限の原則に従った RBAC 設定
- [ ] Azure Security Center の推奨事項を確認

### 5. 監視とアラート

- [ ] Application Insights が有効になっている
- [ ] ログレベルが適切に設定されている（本番: Information）
- [ ] カスタムメトリクスが設定されている
- [ ] コスト予算とアラートが設定されている
  - 月額予算: $10
  - アラート閾値: 50%, 75%, 90%
- [ ] パフォーマンスアラートが設定されている
  - 応答時間 > 2秒
  - エラー率 > 5%
- [ ] 可用性テストが設定されている
- [ ] アラート通知先が設定されている（メール、SMS、Teams など）

### 6. データ移行（初回デプロイのみ）

- [ ] SQL Server からのデータエクスポートが完了している
- [ ] データ形式の変換が完了している
- [ ] Cosmos DB へのデータインポートが完了している
- [ ] Blob Storage への画像アップロードが完了している
- [ ] データの整合性が確認されている
- [ ] 移行前後のデータ件数が一致している
- [ ] バックアップが取得されている

### 7. ドキュメント

- [ ] README.md が最新の状態になっている
- [ ] COSMOS_DB_MIGRATION.md が作成されている
- [ ] INTEGRATION_TESTS.md が作成されている
- [ ] DEPLOYMENT_CHECKLIST.md（このファイル）が作成されている
- [ ] API ドキュメントが更新されている
- [ ] アーキテクチャ図が更新されている
- [ ] トラブルシューティングガイドが作成されている
- [ ] 運用手順書が作成されている

### 8. パフォーマンス

- [ ] クエリのパフォーマンステストが完了している
- [ ] 負荷テストが完了している（想定同時アクセス数）
- [ ] インデックスが最適化されている
- [ ] 画像の遅延読み込みが実装されている
- [ ] CDN の設定が完了している（オプション）
- [ ] キャッシュ戦略が実装されている

## 🚀 デプロイ手順

### ステップ1: ステージング環境へのデプロイ

#### API Functions App
```bash
# ビルド
cd api
dotnet build --configuration Release

# デプロイ
func azure functionapp publish comical-api-staging
```

#### Batch Functions App
```bash
# ビルド
cd batch
dotnet build --configuration Release

# デプロイ
func azure functionapp publish comical-batch-staging
```

#### Static Web App
```bash
# ビルド
cd front
npm run build

# デプロイ（GitHub Actions 経由で自動デプロイされる）
# または手動デプロイ:
swa deploy ./dist/front --env staging
```

### ステップ2: ステージング環境での検証

#### スモークテスト
```bash
# API ヘルスチェック
curl https://comical-api-staging.azurewebsites.net/api/health

# GetComics API テスト
curl -X POST https://comical-api-staging.azurewebsites.net/api/ComicData?fromdate=2024-01-01 \
  -H "Content-Type: application/json" \
  -d '{"SearchList":["test"]}'

# ConfigMigration API テスト
curl -X POST https://comical-api-staging.azurewebsites.net/api/ConfigMigration \
  -H "Content-Type: application/json" \
  -d '["keyword1", "keyword2"]'
```

#### フロントエンドテスト
1. ステージング環境にアクセス: `https://staging.manrem.devtakas.jp`
2. 検索機能のテスト
3. 画像表示のテスト
4. カレンダー登録機能のテスト
5. エラーハンドリングのテスト

#### パフォーマンステスト
```bash
# Apache Bench で負荷テスト
ab -n 1000 -c 10 https://comical-api-staging.azurewebsites.net/api/ComicData

# または Azure Load Testing を使用
```

### ステップ3: 本番環境へのデプロイ

#### Blue-Green デプロイ方式

1. **ステージングスロットへデプロイ**
```bash
# API Functions App
func azure functionapp publish comical-api-prod --slot staging

# Batch Functions App
func azure functionapp publish comical-batch-prod --slot staging
```

2. **スロットで動作確認**
```bash
# ステージングスロットのエンドポイントでテスト
curl https://comical-api-prod-staging.azurewebsites.net/api/ComicData
```

3. **本番スロットにスワップ**
```bash
az functionapp deployment slot swap \
  --name comical-api-prod \
  --resource-group ComiCal-RG \
  --slot staging \
  --target-slot production

az functionapp deployment slot swap \
  --name comical-batch-prod \
  --resource-group ComiCal-RG \
  --slot staging \
  --target-slot production
```

4. **本番環境で動作確認**
```bash
# 本番エンドポイントでテスト
curl https://comical-api-prod.azurewebsites.net/api/ComicData
```

### ステップ4: 本番環境での検証

#### 即時確認項目
- [ ] API が応答している（200 OK）
- [ ] フロントエンドが正常に表示される
- [ ] 検索機能が動作する
- [ ] 画像が正常に表示される
- [ ] エラーログに異常がない

#### 監視（デプロイ後24時間）
- [ ] Application Insights でエラー率を確認
- [ ] 応答時間が正常範囲内（< 2秒）
- [ ] Cosmos DB RU消費量が予想範囲内
- [ ] Blob Storage トランザクションが正常
- [ ] コストが予算内

## 🔄 ロールバック手順

問題が発生した場合のロールバック手順:

### 方法1: デプロイスロットのスワップを元に戻す

```bash
# 即座に前のバージョンに戻す
az functionapp deployment slot swap \
  --name comical-api-prod \
  --resource-group ComiCal-RG \
  --slot staging \
  --target-slot production
```

### 方法2: 前のバージョンを再デプロイ

```bash
# Git で前のバージョンをチェックアウト
git checkout <previous-version-tag>

# 再ビルドと再デプロイ
cd api && func azure functionapp publish comical-api-prod
cd batch && func azure functionapp publish comical-batch-prod
```

### 方法3: Azure Portal から以前のデプロイを選択

1. Azure Portal → Function App → Deployment Center
2. 以前のデプロイを選択
3. "Redeploy" をクリック

## 📊 デプロイ後の監視

### 初日（デプロイ後24時間）

チェック頻度: 1時間ごと
- Application Insights ダッシュボードを確認
- エラーログを確認
- パフォーマンスメトリクスを確認
- ユーザーフィードバックを収集

### 1週間

チェック頻度: 1日1回
- コスト使用状況を確認
- パフォーマンストレンドを分析
- ユーザー利用状況を分析
- アラートを確認

### 継続的な監視

チェック頻度: 週1回
- 月次コストレポートを確認
- パフォーマンスの最適化機会を特定
- セキュリティアラートを確認
- バックアップの整合性を確認

## 📞 緊急連絡先

デプロイ中の問題発生時の連絡先:

| 役割 | 担当者 | 連絡先 |
|------|--------|--------|
| プロジェクトオーナー | [名前] | [メール/電話] |
| インフラ担当 | [名前] | [メール/電話] |
| 開発リード | [名前] | [メール/電話] |
| Azure サポート | Microsoft | Azure Portal からケース作成 |

## 📝 デプロイ記録

各デプロイの記録を残す:

| 日付 | 環境 | バージョン | 担当者 | 結果 | 備考 |
|------|------|-----------|--------|------|------|
| YYYY-MM-DD | Staging | v1.0.0 | [名前] | 成功 | 初回デプロイ |
| YYYY-MM-DD | Production | v1.0.0 | [名前] | 成功 | 本番リリース |

## ✅ 完了確認

すべてのチェックリスト項目が完了したら:

- [ ] デプロイ完了報告書を作成
- [ ] ステークホルダーに通知
- [ ] ドキュメントを更新
- [ ] 次回のデプロイ計画を作成

---

**注意**: このチェックリストは一般的なガイドラインです。プロジェクトの特性に応じてカスタマイズしてください。
