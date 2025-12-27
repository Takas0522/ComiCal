## 概要
API層のComicService を更新し、メモリ内検索をCosmos DBクエリに移行

## 対象ファイル
- `api/Comical.Api/Services/Comic/ComicService.cs`
- `api/Comical.Api/Services/Comic/IComicService.cs`

## 作業内容
1. `GetComics` メソッドの更新:
   - メモリ内検索 (Title/Author Contains) を Cosmos DB クエリに移行
   - SQL: `WHERE CONTAINS(c.title, @keyword) OR CONTAINS(c.author, @keyword)`
   - `ORDER BY c.salesDate DESC`
   - 複数キーワード対応（AND条件）
2. `GetComicImages` メソッドの削除または統合
3. `ImageUrlHelper` を使用した画像URL生成ロジックの追加

## 依存関係
- **前提**: Phase 2-1 完了（API Repository実装）
- **前提**: Phase 1-2 完了（ImageUrlHelper実装）
- **後続**: Phase 4（フロントエンド対応）

## 完了条件
- [ ] Cosmos DBクエリが実装されている
- [ ] 検索パフォーマンスが向上している
- [ ] 画像URL生成が正しく動作する
- [ ] ビルドエラーがない
