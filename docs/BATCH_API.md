# Batch Manual Execution API Documentation

## 概要

このドキュメントでは、Container Jobsの手動実行とページ範囲指定部分再実行機能を提供するHTTP APIについて説明します。

## 認証

すべてのAPIエンドポイントはAPI Key認証が必要です。

### API Keyの提供方法

1. **HTTPヘッダー（推奨）**
   ```
   X-API-Key: your-api-key-here
   ```

2. **クエリパラメータ**
   ```
   ?api_key=your-api-key-here
   ```

### API Keyの設定

環境変数 `BATCH_API_KEY` にAPI Keyを設定してください。

**ローカル開発:**
`local.settings.json` に設定
```json
{
  "Values": {
    "BATCH_API_KEY": "dev-api-key-change-in-production"
  }
}
```

**Azure環境:**
Container Appsの環境変数として設定
```bash
az containerapp update \
  --name comical-batch \
  --resource-group comical-rg \
  --set-env-vars BATCH_API_KEY=secretref:batch-api-key
```

## エンドポイント

### 1. データ登録Job手動実行

コミックデータを楽天Books APIから取得してPostgreSQLに登録するJobを手動で実行します。

**エンドポイント:** `POST /api/batch/registration`

**認証:** 必須

**リクエスト例:**
```bash
curl -X POST https://your-app.azurecontainerapps.io/api/batch/registration \
  -H "X-API-Key: your-api-key-here"
```

**レスポンス:**
```json
{
  "success": true,
  "message": "Registration job triggered successfully. Job is now running in the background.",
  "batchId": 123,
  "jobType": "DataRegistration",
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**エラーレスポンス:**
```json
{
  "success": false,
  "message": "Job cannot proceed: Manual intervention required - batch is paused",
  "batchId": 123,
  "jobType": "DataRegistration",
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**HTTPステータスコード:**
- `200 OK` - Job実行成功
- `400 Bad Request` - Job実行失敗（条件を満たしていない）
- `401 Unauthorized` - API Key認証失敗
- `500 Internal Server Error` - サーバーエラー

---

### 2. 画像ダウンロードJob手動実行

楽天Books APIから画像をダウンロードしてBlob Storageに保存するJobを手動で実行します。

**エンドポイント:** `POST /api/batch/images`

**認証:** 必須

**前提条件:** データ登録フェーズが完了している必要があります。

**リクエスト例:**
```bash
curl -X POST https://your-app.azurecontainerapps.io/api/batch/images \
  -H "X-API-Key: your-api-key-here"
```

**レスポンス:**
```json
{
  "success": true,
  "message": "Image download job triggered successfully. Job is now running in the background.",
  "batchId": 123,
  "jobType": "ImageDownload",
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**エラーレスポンス（依存関係エラー）:**
```json
{
  "success": false,
  "message": "Job cannot proceed: Registration phase must be completed before image download can proceed",
  "batchId": 123,
  "jobType": "ImageDownload",
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**HTTPステータスコード:**
- `200 OK` - Job実行成功
- `400 Bad Request` - Job実行失敗（依存関係や条件を満たしていない）
- `401 Unauthorized` - API Key認証失敗
- `500 Internal Server Error` - サーバーエラー

---

### 3. ページ範囲指定部分再実行

特定のページ範囲のみを再実行します。エラーが発生したページの再処理に使用します。

**エンドポイント:** `POST /api/batch/registration/partial`

**認証:** 必須

**クエリパラメータ:**
- `startPage` (必須, integer): 開始ページ番号（1以上）
- `endPage` (必須, integer): 終了ページ番号（startPage以上）

**リクエスト例:**
```bash
curl -X POST "https://your-app.azurecontainerapps.io/api/batch/registration/partial?startPage=5&endPage=10" \
  -H "X-API-Key: your-api-key-here"
```

**レスポンス:**
```json
{
  "success": true,
  "message": "Partial retry triggered for pages 5-10 (6 pages). Job is now running in the background.",
  "batchId": 123,
  "startPage": 5,
  "endPage": 10,
  "pageCount": 6,
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**エラーレスポンス（パラメータエラー）:**
```json
{
  "error": "Missing required parameters",
  "details": "Both startPage and endPage query parameters are required",
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**エラーレスポンス（バリデーションエラー）:**
```json
{
  "success": false,
  "message": "Invalid page range: 10-5. Start page must be >= 1 and end page must be >= start page.",
  "batchId": null,
  "pageCount": 0,
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**HTTPステータスコード:**
- `200 OK` - 部分再実行成功
- `400 Bad Request` - パラメータエラーまたはバリデーションエラー
- `401 Unauthorized` - API Key認証失敗
- `500 Internal Server Error` - サーバーエラー

---

### 4. 手動介入解除・自動復帰設定

手動介入フラグをクリアし、バッチ処理の自動復帰を有効化します。

**エンドポイント:** `POST /api/batch/reset-intervention`

**認証:** 必須

**クエリパラメータ:**
- `batchId` (オプション, integer): 対象のBatch ID。省略時は当日のバッチを対象とします。

**リクエスト例（当日のバッチ）:**
```bash
curl -X POST https://your-app.azurecontainerapps.io/api/batch/reset-intervention \
  -H "X-API-Key: your-api-key-here"
```

**リクエスト例（特定のバッチ）:**
```bash
curl -X POST "https://your-app.azurecontainerapps.io/api/batch/reset-intervention?batchId=123" \
  -H "X-API-Key: your-api-key-here"
```

**レスポンス:**
```json
{
  "success": true,
  "message": "Manual intervention cleared for batch 123. Job will auto-resume on next scheduled run.",
  "batchId": 123,
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**エラーレスポンス（バッチが見つからない）:**
```json
{
  "error": "Batch not found",
  "details": "No batch found for today. Provide a specific batchId query parameter.",
  "timestamp": "2026-01-01T12:00:00Z"
}
```

**HTTPステータスコード:**
- `200 OK` - 手動介入解除成功
- `404 Not Found` - バッチが見つからない
- `401 Unauthorized` - API Key認証失敗
- `500 Internal Server Error` - サーバーエラー

---

## セキュリティ

### API Key管理

1. **本番環境では必ず強力なAPI Keyを生成してください**
   ```bash
   # 強力なランダム文字列を生成（例）
   openssl rand -base64 32
   ```

2. **API KeyをGitにコミットしないでください**
   - Azure Key VaultやContainer Appsのシークレット機能を使用してください

3. **定期的にAPI Keyをローテーションしてください**

### セキュリティログ

すべての認証試行は以下の情報とともにログに記録されます：
- タイムスタンプ
- クライアントIPアドレス（X-Forwarded-For / X-Real-IPヘッダーを考慮）
- リクエストパス
- 認証結果（成功/失敗）

失敗したリクエストの例：
```
[Warning] API Key authentication failed: Invalid API key provided. IP: 192.168.1.100, Path: /api/batch/registration
```

---

## 使用例

### シナリオ1: 毎日の定期実行に加えて手動で追加実行

```bash
# データ登録を手動実行
curl -X POST https://your-app.azurecontainerapps.io/api/batch/registration \
  -H "X-API-Key: ${BATCH_API_KEY}"

# 登録完了後、画像ダウンロードを実行
curl -X POST https://your-app.azurecontainerapps.io/api/batch/images \
  -H "X-API-Key: ${BATCH_API_KEY}"
```

### シナリオ2: エラーページの部分再実行

```bash
# ページ50-100でエラーが発生した場合の再実行
curl -X POST "https://your-app.azurecontainerapps.io/api/batch/registration/partial?startPage=50&endPage=100" \
  -H "X-API-Key: ${BATCH_API_KEY}"
```

### シナリオ3: 手動介入状態からの復帰

```bash
# 問題を解決後、手動介入フラグをクリア
curl -X POST https://your-app.azurecontainerapps.io/api/batch/reset-intervention \
  -H "X-API-Key: ${BATCH_API_KEY}"
```

---

## トラブルシューティング

### 401 Unauthorized

**原因:** API Keyが無効または欠落している

**対処:**
1. API Keyが正しく設定されているか確認
2. ヘッダー名が正しいか確認（`X-API-Key`）
3. 環境変数 `BATCH_API_KEY` が設定されているか確認

### 400 Bad Request - "Manual intervention required"

**原因:** バッチが手動介入待ち状態にある

**対処:**
1. バッチ状態を確認
2. 問題を解決
3. `/api/batch/reset-intervention` で手動介入フラグをクリア

### 400 Bad Request - "Registration phase must be completed"

**原因:** 画像ダウンロードJobの実行前にデータ登録Jobが完了していない

**対処:**
1. `/api/batch/registration` でデータ登録Jobを先に実行
2. 完了を待ってから画像ダウンロードJobを実行

---

## 監視とログ

### Application Insights

すべてのAPI呼び出しは Application Insights に記録されます：

**カスタムメトリクス:**
- `BatchApi.TriggerRegistration` - 登録Job実行回数
- `BatchApi.TriggerImageDownload` - 画像DLJob実行回数
- `BatchApi.PartialRetry` - 部分再実行回数
- `BatchApi.ResetIntervention` - 手動介入解除回数

**カスタムイベント:**
- `BatchApiCall` - すべてのAPI呼び出し
- `BatchApiAuthFailure` - 認証失敗

**ログクエリ例（Application Insights）:**
```kusto
traces
| where message contains "API Key authentication"
| project timestamp, severityLevel, message
| order by timestamp desc
```

---

## 制限事項

1. **同時実行制限**: Container Jobsは同時に1つのみ実行可能です
2. **レート制限**: 楽天Books APIのレート制限（30秒/リクエスト）に準拠します
3. **タイムアウト**: 長時間実行時はHTTPリクエストがタイムアウトする可能性がありますが、Jobはバックグラウンドで継続実行されます
4. **ページ範囲**: 部分再実行は現在「registration」フェーズのみサポートされています

---

## Container Apps外部アクセス設定

### Ingressの有効化

```bash
az containerapp ingress enable \
  --name comical-batch \
  --resource-group comical-rg \
  --type external \
  --target-port 80 \
  --transport http
```

### カスタムドメインの設定（オプション）

```bash
az containerapp hostname add \
  --name comical-batch \
  --resource-group comical-rg \
  --hostname batch.yourdomain.com
```

---

## 参考リンク

- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Azure Functions HTTP Triggers](https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook)
- [Application Insights Monitoring](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
