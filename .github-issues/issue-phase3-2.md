## 概要
Batch層のComicService を更新し、画像管理をBlob Storage単体に変更

## 対象ファイル
- `batch/ComiCal.Batch/Services/Comic/ComicService.cs`
- `batch/ComiCal.Batch/Services/Comic/IComicService.cs`

## 作業内容
1. `RegitoryAsync` メソッドの更新:
   - `ComicImage` テーブル操作を削除
   - `Comic` のみを Cosmos DB に Upsert
   - 画像URL変更検知ロジックを削除（Blob Storage側で管理）
2. `UpdateImageDataAsync` メソッドの更新:
   - Content-Type から拡張子を判定 (`ContentTypeHelper` 使用)
   - Blob Storage パス: `/images/{isbn}.{ext}`
   - データベースへの URL 保存処理を削除
3. `GetUpdateImageTargetAsync` メソッドの実装変更:
   - Cosmos DB から画像未設定判定ロジックを削除
   - Blob Storage の存在チェックベースに変更（または別の方法）

## 依存関係
- **前提**: Phase 2-3 完了（Batch Repository実装）
- **前提**: Phase 1-3 完了（ContentTypeHelper実装）
- **後続**: Phase 4-2（統合テスト）

## 完了条件
- [ ] 画像がBlob Storageに正しい命名規則で保存される
- [ ] Cosmos DBへの画像URL保存処理が削除されている
- [ ] ビルドエラーがない
