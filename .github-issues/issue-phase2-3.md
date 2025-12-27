## 概要
Batch層のComic用Repository を Cosmos DB SDK で実装

## 対象ファイル
- `batch/ComiCal.Batch/Repositories/Comic/ComicRepository.cs`
- `batch/ComiCal.Batch/Repositories/Comic/IComicRepository.cs`

## 作業内容
1. `IComicRepository` インターフェースを Cosmos DB用に更新:
   - `GetComicsAsync()` : 全件取得（バッチ処理用）
   - `UpsertComicsAsync(IEnumerable<Comic> comics)` : Bulk Upsert
   - 不要なメソッド削除: `RegisterComicsAsync`, `GetComicImageInfo`, `RegisterComicImageUrlAsync`, `GetUpdateImageTargetAsync`
2. `ComicRepository` を Cosmos DB SDK で実装:
   - Bulk Executor パターンを使用した高速Upsert
   - 並列処理の実装
   - エラーハンドリング

## 依存関係
- **前提**: Phase 1-2 完了（Cosmos DB接続プロバイダー）
- **後続**: Phase 3-2 (Batch層Service更新)

## 完了条件
- [ ] Cosmos DB Bulk API を使用した実装が完了
- [ ] 並列処理が適切に実装されている
- [ ] ビルドエラーがない
