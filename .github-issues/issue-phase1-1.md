## 概要
Comic と ComicImage を単一の Cosmos DB エンティティに統合し、画像情報をBlob Storageで管理する形式に変更

## 対象ファイル
- `ComiCal.Server/ComiCal.Shared/Models/Comic.cs`
- `ComiCal.Server/ComiCal.Shared/Models/ComicImage.cs` (削除予定)

## 作業内容
1. `Comic.cs` から `ImageStorageUrl` プロパティを削除
2. Cosmos DB用のプロパティ追加:
   - `id` (string): Isbn をそのまま id として使用
   - `type` (string): 固定値 "comic" を設定
3. `ComicImage.cs` ファイルを削除
4. `IComicImageRepository` インターフェースと実装を削除

## 依存関係
- **前提**: なし（最初のタスク）
- **後続**: 全てのPhase 1タスク完了後にPhase 2を開始

## 完了条件
- [ ] Comic モデルが Cosmos DB エンティティとして適切な構造になっている
- [ ] ComicImage 関連のファイルが削除されている
- [ ] ビルドエラーが発生しない（他のタスクでの修正が必要な箇所は後続タスクで対応）
