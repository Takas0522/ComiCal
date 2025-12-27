## 概要
Cosmos DB接続用のプロバイダーとユーティリティを実装し、画像URLを動的生成する機能を追加

## 対象ファイル
- `ComiCal.Server/ComiCal.Shared/Providers/ConnectionProvider.cs`
- 新規: `ComiCal.Server/ComiCal.Shared/Util/ImageUrlHelper.cs`

## 作業内容
1. `ConnectionProvider.cs` に Cosmos DB接続用のファクトリーメソッドを追加:
   - `CosmosClientFactory` delegate
   - Cosmos DB接続文字列の設定キー定義
2. 画像URL生成ヘルパークラス `ImageUrlHelper` を作成:
   - `GetImageUrl(string blobBaseUrl, string isbn, string extension)` メソッド
   - パスフォーマット: `/images/{isbn}.{ext}`
3. NuGetパッケージ追加:
   - `Microsoft.Azure.Cosmos` (最新安定版)

## 依存関係
- **前提**: Phase 1-1 完了
- **後続**: Phase 2 の全Repository実装

## 完了条件
- [ ] CosmosClient のファクトリーメソッドが定義されている
- [ ] ImageUrlHelper が実装され、単体テストが通る
- [ ] NuGetパッケージが全プロジェクトに追加されている
