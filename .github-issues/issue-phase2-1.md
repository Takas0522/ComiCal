## 概要
API層のComic用Repository を Cosmos DB SDK で実装

## 対象ファイル
- `api/Comical.Api/Repositories/Comic/ComicRepository.cs`
- `api/Comical.Api/Repositories/Comic/IComicRepository.cs`

## 作業内容
1. `IComicRepository` インターフェースを Cosmos DB用に更新:
   - `GetComicsAsync(DateTime fromDate)` : SQL API クエリ使用
   - 不要なメソッド削除: `GetComicImages`, `GetComicImageInfo`, `RegisterComicsAsync`, `RegisterComicImageUrlAsync`, `GetUpdateImageTargetAsync`
2. `ComicRepository` を Cosmos DB SDK で実装:
   - `CosmosClient` をコンストラクタインジェクション
   - コンテナ名: "comics"
   - パーティションキー: `/id`
   - クエリ実装: `WHERE c.type = "comic" AND c.salesDate >= @fromDate`
   - 継続トークンによるページング実装

## 依存関係
- **前提**: Phase 1-2 完了（Cosmos DB接続プロバイダー）
- **後続**: Phase 3-1 (API層Service更新)

## 完了条件
- [ ] Cosmos DB SDK を使用した実装が完了
- [ ] インターフェースが更新され、不要なメソッドが削除されている
- [ ] ビルドエラーがない
