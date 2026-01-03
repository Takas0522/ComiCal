# Batch Job Coordination Services

このディレクトリには、バッチジョブの実行状態管理、リトライ制御、部分再実行機能を提供する共通サービスが含まれています。

## サービス概要

### 1. IBatchStateService / BatchStateService
バッチ実行状態の基本的なCRUD操作を提供するサービス。

**主な機能:**
- バッチ状態の取得・作成
- ステータス更新（pending, running, completed, failed, delayed, manual_intervention）
- フェーズ状態の更新（registration, image_download）
- 進捗管理（処理済みページ数、エラーページ数）
- エラー記録と解決管理

**使用例:**
```csharp
// バッチ状態の取得または作成
var batchState = await _batchStateService.GetOrCreateBatchStateAsync(DateTime.Today);

// ステータスの更新
await _batchStateService.UpdateBatchStatusAsync(batchState.Id, BatchStatus.Running);

// フェーズの更新
await _batchStateService.UpdatePhaseStatusAsync(batchState.Id, BatchPhase.Registration, PhaseStatus.Running);

// 進捗の更新
await _batchStateService.UpdateProgressAsync(batchState.Id, processedPages: 10, failedPages: 2);

// エラーの記録
await _batchStateService.RecordPageErrorAsync(
    batchState.Id, 
    pageNumber: 5, 
    BatchPhase.Registration, 
    "HttpRequestException", 
    "API rate limit exceeded");
```

### 2. JobSchedulingService
ジョブのスケジューリング、遅延制御、依存関係管理を行うサービス。

**主な機能:**
- **3回遅延上限制御**: 自動リトライは最大3回まで（5分、15分、30分の遅延）
- **Job間依存関係チェック**: 画像ダウンロードは登録完了後にのみ実行可能
- **手動介入管理**: 最大リトライ回数到達時に手動介入フラグを設定
- **自動復帰制御**: 手動介入解除後の自動再開

**使用例:**
```csharp
// ジョブ実行可否のチェック
var (canProceed, reason) = await _jobSchedulingService.CanJobProceedAsync(batchId, BatchPhase.ImageDownload);
if (!canProceed)
{
    _logger.LogWarning("Cannot proceed: {Reason}", reason);
    return;
}

// ジョブ失敗時の処理（自動リトライ）
try
{
    // ジョブ実行
}
catch (Exception ex)
{
    var canRetry = await _jobSchedulingService.HandleJobFailureAsync(batchId, phase, ex);
    if (!canRetry)
    {
        // 最大リトライ回数到達 - 手動介入が必要
        _logger.LogError("Manual intervention required for batch {BatchId}", batchId);
    }
}

// 再開可能なバッチの取得
var readyBatches = await _jobSchedulingService.GetBatchesReadyToResumeAsync();

// 手動介入のクリア
await _jobSchedulingService.ClearManualInterventionAsync(batchId);
```

### 3. PartialRetryService
部分再実行とチェックポイント管理を提供するサービス。

**主な機能:**
- **ページ範囲指定のリトライ**: 特定のページ範囲のみを再実行
- **エラーページのみ再実行**: エラーが発生したページのみを対象に再実行
- **進捗チェックポイント管理**: 処理済みページとエラーページの追跡
- **リトライ統計**: バッチの実行状況と再試行情報の取得

**使用例:**
```csharp
// エラーページの取得
var errorPages = await _partialRetryService.GetErrorPagesAsync(batchId, BatchPhase.Registration);
_logger.LogInformation("Found {Count} error pages: {Pages}", errorPages.Count(), string.Join(", ", errorPages));

// エラーページのリセット（再実行準備）
await _partialRetryService.ResetErrorPagesAsync(batchId, BatchPhase.Registration);

// ページ範囲のリセット
await _partialRetryService.ResetPageRangeAsync(batchId, startPage: 10, endPage: 20, BatchPhase.Registration);

// チェックポイントの記録
await _partialRetryService.MarkCheckpointAsync(batchId, processedPages: 50, failedPages: 3);

// リトライ統計の取得
var stats = await _partialRetryService.GetRetryStatisticsAsync(batchId);
_logger.LogInformation(
    "Batch {BatchId}: {Processed}/{Total} pages, {Failed} failed, {CanRetry} can retry",
    stats.BatchId, stats.ProcessedPages, stats.TotalPages, stats.FailedPages, stats.CanRetry);

// 完全リトライのためのリセット
await _partialRetryService.ResetBatchForFullRetryAsync(batchId);
```

## データベーススキーマ

### batch_states テーブル
バッチの実行状態を管理します。

**主要カラム:**
- `id`: バッチID（主キー）
- `batch_date`: バッチ実行日（ユニーク）
- `status`: バッチステータス（pending, running, completed, failed, delayed, manual_intervention）
- `total_pages`: 総ページ数
- `processed_pages`: 処理済みページ数
- `failed_pages`: エラーページ数
- `registration_phase`: 登録フェーズのステータス
- `image_download_phase`: 画像ダウンロードフェーズのステータス
- `delayed_until`: 遅延終了時刻
- `retry_attempts`: リトライ回数
- `manual_intervention_required`: 手動介入フラグ
- `auto_resume_enabled`: 自動再開有効フラグ

### batch_page_errors テーブル
ページレベルのエラーを記録します。

**主要カラム:**
- `id`: エラーID（主キー）
- `batch_id`: バッチID（外部キー）
- `page_number`: ページ番号
- `phase`: フェーズ（registration, image_download）
- `error_type`: エラータイプ
- `error_message`: エラーメッセージ
- `retry_count`: リトライ回数
- `resolved`: 解決済みフラグ

## リトライ戦略

### 自動リトライ（最大3回）
1. **1回目のリトライ**: エラー発生から5分後
2. **2回目のリトライ**: エラー発生から15分後（累計20分）
3. **3回目のリトライ**: エラー発生から30分後（累計50分）

3回のリトライ後も失敗する場合は、`manual_intervention_required`フラグが設定され、手動介入が必要になります。

### 依存関係
- 画像ダウンロードフェーズは、登録フェーズが完了するまで実行できません
- `JobSchedulingService.CanJobProceedAsync()`で依存関係をチェックします

## エラーハンドリング

すべてのサービスは適切なログ出力を行います：
- **Debug**: 詳細な実行状況
- **Information**: 重要なイベント（バッチ作成、ステータス変更など）
- **Warning**: エラー記録、手動介入の設定など
- **Error**: 致命的なエラー、最大リトライ回数到達など

## Application Insights統合

将来的に、これらのサービスはApplication Insightsと統合され、以下のメトリクスを追跡します：
- バッチ実行時間
- リトライ回数
- エラー率
- フェーズ別成功率

（Application Insightsの実装はStep 8で行われます）

## テスト

現在、リポジトリにはテストインフラが存在しませんが、すべてのサービスはSOLID原則に従って設計されており、単体テストが容易です：

- リポジトリパターンにより、データアクセスのモック化が可能
- インターフェース駆動設計により、依存性注入とモック化が容易
- 各サービスは単一責任を持ち、テストが容易

## DIへの登録

`Program.cs`で以下のように登録されています：

```csharp
services.AddSingleton<IBatchStateRepository, BatchStateRepository>();
services.AddSingleton<IBatchStateService, BatchStateService>();
services.AddSingleton<JobSchedulingService>();
services.AddSingleton<PartialRetryService>();
```

## 関連ドキュメント
- [データベーススキーマ](/src/database/init.sql) - batch_states と batch_page_errors テーブルの定義
- [Step 2: データベーススキーマ更新](../../../docs/) - スキーマ設計の背景
