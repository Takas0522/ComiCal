## 概要
設定ファイルに Cosmos DB 接続文字列とBlob URL設定を追加

## 対象ファイル
- `api/local.settings.json`
- `batch/ComiCal.Batch/local.settings.json`
- `front/src/environments/environment.ts`
- `front/src/environments/environment.prod.ts`

## 作業内容
1. API/Batch層の設定:
   - `CosmosDbConnectionString` キーを追加
   - `CosmosDbDatabaseName` キーを追加（値: "comical"）
   - 既存の `StorageConnectionString` を維持
2. フロントエンドの設定:
   - `blobBaseUrl` を environment に追加
   - 例: `https://<storage-account>.blob.core.windows.net/<container>`
3. Startup.cs の DI 登録更新:
   - `CosmosClient` をシングルトン登録
   - コンテナへの参照を取得

## 依存関係
- **前提**: Phase 4-2 完了（コンテナ初期化）
- **後続**: Phase 4-4（統合テスト）

## 完了条件
- [ ] 全ての設定ファイルが更新されている
- [ ] Startup.cs で CosmosClient が DI 登録されている
- [ ] 環境変数のテンプレートがドキュメント化されている
