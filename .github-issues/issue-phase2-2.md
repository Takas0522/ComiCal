## 概要
API層のConfigMigration用Repository を Cosmos DB SDK で実装

## 対象ファイル
- `api/Comical.Api/Repositories/ConfigMigration/ConfigMigrationRepository.cs`
- `api/Comical.Api/Repositories/ConfigMigration/IConfigMigrationRepository.cs`

## 作業内容
1. `IConfigMigrationRepository` インターフェースを維持（変更不要の可能性大）
2. `ConfigMigrationRepository` を Cosmos DB SDK で実装:
   - `CosmosClient` をコンストラクタインジェクション
   - コンテナ名: "config-migrations"
   - パーティションキー: `/id`
   - `GetConfigMigration(string id)` : ポイント読み取り (1 RU)
   - `RegisterConfigMigrationData(ConfigMigration data)` : Upsert
   - `DeleteConfigMigration(string id)` : Delete

## 依存関係
- **前提**: Phase 1-2 完了（Cosmos DB接続プロバイダー）
- **後続**: Phase 3-1 (API層Service更新)

## 完了条件
- [ ] Cosmos DB SDK を使用した実装が完了
- [ ] CRUD操作が正しく実装されている
- [ ] ビルドエラーがない
