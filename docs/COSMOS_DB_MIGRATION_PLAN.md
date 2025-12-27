# Cosmos DB Migration - Development Plan

## Overview
このドキュメントは、ComiCal システムを SQL Server から Cosmos DB に移行するための開発計画を示しています。

## Architecture Changes

### Before (SQL Server)
- Comic テーブル + ComicImage テーブル (1:1関係)
- 画像URLをデータベースに保存
- SQL Server での結合クエリ

### After (Cosmos DB)
- Comic ドキュメント（単一エンティティ、パーティションキー: `/id` = ISBN）
- 画像はBlob Storageで `/images/{isbn}.{ext}` 形式で管理
- 画像URLは動的生成
- サーバーレスモードで月額約$25を想定

## Development Phases

### Phase 1: 基盤タスク（3タスク）
すべてのタスクの前提となる共通基盤の実装

1. **[Phase 1-1] データモデルの簡素化**
   - Comic と ComicImage の統合
   - Cosmos DB用プロパティ追加（id, type）
   - 依存: なし
   - 期間: 0.5日

2. **[Phase 1-2] Cosmos DB接続プロバイダーと画像URLヘルパー実装**
   - CosmosClient ファクトリーメソッド追加
   - ImageUrlHelper 実装
   - NuGetパッケージ追加: Microsoft.Azure.Cosmos
   - 依存: Phase 1-1
   - 期間: 1日

3. **[Phase 1-3] Content-Type判定ユーティリティ実装**
   - ContentTypeHelper 実装
   - 画像拡張子マッピング
   - 依存: Phase 1-1
   - 期間: 0.5日

### Phase 2: Repository層（3タスク）
データアクセス層の Cosmos DB 実装

4. **[Phase 2-1] API層 Comic Repository の Cosmos DB実装**
   - IComicRepository インターフェース更新
   - Cosmos DB SDK 実装
   - クエリとページング実装
   - 依存: Phase 1-2
   - 期間: 2日

5. **[Phase 2-2] API層 ConfigMigration Repository の Cosmos DB実装**
   - Cosmos DB SDK 実装（CRUD操作）
   - 依存: Phase 1-2
   - 期間: 1日

6. **[Phase 2-3] Batch層 Comic Repository の Cosmos DB実装**
   - Bulk Executor パターン実装
   - 並列処理実装
   - 依存: Phase 1-2
   - 期間: 2日

### Phase 3: Service層とフロントエンド（3タスク）
ビジネスロジックとUI層の更新

7. **[Phase 3-1] API層 Comic Service の更新**
   - メモリ内検索をCosmos DBクエリに移行
   - ImageUrlHelper統合
   - 依存: Phase 2-1, Phase 1-2
   - 期間: 1.5日

8. **[Phase 3-2] Batch層 Comic Service の更新**
   - 画像管理のBlob Storage化
   - ContentTypeHelper統合
   - 依存: Phase 2-3, Phase 1-3
   - 期間: 1.5日

9. **[Phase 4-1] フロントエンド画像パス動的生成と404対応**
   - 画像URL動的生成実装
   - 404時「画像なし」表示
   - 依存: Phase 3-1
   - 期間: 1日

### Phase 4: 統合・設定タスク（3タスク）
インフラ設定と最終統合

10. **[Phase 4-2] Cosmos DB コンテナ初期化スクリプト作成**
    - セットアップスクリプト作成
    - インデックスポリシー設定
    - 依存: Phase 1-2
    - 期間: 1日

11. **[Phase 4-3] 設定ファイル更新とDI登録**
    - 接続文字列設定
    - Startup.cs DI登録
    - 依存: Phase 4-2
    - 期間: 0.5日

12. **[Phase 4-4] 統合テストとドキュメント更新**
    - 統合テスト実施
    - ドキュメント更新
    - 依存: 全タスク完了
    - 期間: 2日

## Task Dependencies Graph

```
Phase 1-1 (データモデル)
    ├─→ Phase 1-2 (接続プロバイダー)
    │       ├─→ Phase 2-1 (API Comic Repo)
    │       │       └─→ Phase 3-1 (API Comic Service)
    │       │               └─→ Phase 4-1 (フロントエンド)
    │       ├─→ Phase 2-2 (API ConfigMigration Repo)
    │       ├─→ Phase 2-3 (Batch Comic Repo)
    │       │       └─→ Phase 3-2 (Batch Comic Service) ←─┐
    │       └─→ Phase 4-2 (コンテナ初期化)                  │
    │               └─→ Phase 4-3 (設定ファイル)            │
    └─→ Phase 1-3 (ContentTypeHelper) ─────────────────────┘

全Phase完了 → Phase 4-4 (統合テスト)
```

## Parallel Work Opportunities

### 並行作業可能なグループ:
1. **Phase 1完了後**:
   - Phase 2-1, 2-2, 2-3 は並行実装可能
   - Phase 4-2 も並行開始可能

2. **Phase 2完了後**:
   - Phase 3-1 と Phase 3-2 は並行実装可能

## Estimated Timeline
- **Phase 1**: 2日
- **Phase 2**: 5日（並行作業で2-3日に短縮可能）
- **Phase 3**: 4日（並行作業で2日に短縮可能）
- **Phase 4**: 3.5日
- **合計**: 約14.5日（並行作業で9-10日に短縮可能）

## Cost Estimate (Cosmos DB Serverless)
- **前提**: 50,000ドキュメント、月間100万読み取り、1万書き込み
- **ストレージ**: 100GB × $0.25 = $25
- **RU消費**: (1,000,000 + 100,000) / 1,000,000 × $0.25 ≈ $0.28
- **合計**: 約$25.28/月

## Setup Instructions

### Prerequisites
1. Azure subscription
2. GitHub CLI (`gh`) installed and authenticated
3. .NET 6.0+ SDK
4. Node.js 16+ (for Angular)

### Creating GitHub Issues
1. GitHub CLIで認証:
   ```powershell
   gh auth login
   ```

2. Issueを一括作成:
   ```powershell
   .\scripts\create-github-issues.ps1
   ```

### Development Setup
1. Cosmos DB アカウントを作成（サーバーレスモード）
2. Blob Storage アカウントを作成
3. 設定ファイルに接続文字列を設定
4. コンテナ初期化スクリプトを実行

## References
- [Cosmos DB コスト管理](https://learn.microsoft.com/ja-jp/azure/cosmos-db/plan-manage-costs)
- [Cosmos DB パーティション設計](https://learn.microsoft.com/ja-jp/azure/cosmos-db/partitioning-overview)
- [Cosmos DB .NET SDK](https://learn.microsoft.com/ja-jp/azure/cosmos-db/sql/sql-api-sdk-dotnet-standard)
