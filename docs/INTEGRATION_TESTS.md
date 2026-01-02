# 統合テストガイド

ComiCal プロジェクトの統合テスト手順を説明します。

## 概要

このドキュメントでは、API層、Batch層、フロントエンドの統合テスト手順を提供します。プロジェクトは **.NET 10 LTS + Isolated worker model** を使用しており、Azure Functions Core Tools v4 でテストを実行します。

## 前提条件

### 開発環境

- **.NET 10 LTS + Isolated worker model**
- Azure Functions Core Tools v4
- PostgreSQL 14+
- Node.js 18+
- Angular CLI
- PowerShell 7+（統合テストスクリプト用）

### 環境設定

テストを実行する前に、以下の環境設定が完了していることを確認してください：

1. **local.settings.json の設定**
   - `FUNCTIONS_WORKER_RUNTIME` が `dotnet-isolated` に設定されていること
   - PostgreSQL 接続文字列が正しく設定されていること
   - Storage 接続文字列（Azurite または Azure Storage）が設定されていること

2. **サービスの起動**
   - PostgreSQL が起動していること
   - Azurite が起動していること（ローカル環境）

## 統合テストの実行

### クイックスタート

```powershell
# すべてのテストを実行（ローカル環境）
cd scripts
.\test-integration.ps1 -Environment Local -RunAllTests

# API層のテストのみ実行
.\test-integration.ps1 -Environment Local -TestApi

# Batch層のテストのみ実行
.\test-integration.ps1 -Environment Local -TestBatch

# フロントエンドのテストのみ実行
.\test-integration.ps1 -Environment Local -TestFrontend
```

### 開発環境でのテスト実行

```powershell
# 開発環境でテスト実行
.\test-integration.ps1 -Environment Dev -RunAllTests

# ステージング環境でテスト実行
.\test-integration.ps1 -Environment Staging -RunAllTests
```

### テストパラメータ

| パラメータ | 説明 | デフォルト値 |
|-----------|------|------------|
| `Environment` | テスト環境（Local, Dev, Staging, Prod） | 必須 |
| `RunAllTests` | すべてのテストを実行 | - |
| `TestApi` | API層のテストのみ実行 | - |
| `TestBatch` | Batch層のテストのみ実行 | - |
| `TestFrontend` | フロントエンドのテストのみ実行 | - |
| `ResponseTimeThresholdMs` | APIレスポンスタイムの閾値（ミリ秒） | 2000ms |
| `ConsistencyWaitSeconds` | データベースの整合性確認の待機時間（秒） | 2秒 |

## API層のテスト

### テスト項目

- **Health Check**: API のヘルスチェックエンドポイント
- **Comics CRUD**: 漫画データの作成、読み取り、更新、削除
- **Search**: 漫画検索機能
- **Response Time**: APIレスポンスタイムの測定

### ローカル実行

```bash
# Azure Functions を起動
cd src/ComiCal.Server/Comical.Api
func start

# 別のターミナルでテスト実行
cd scripts
.\test-integration.ps1 -Environment Local -TestApi
```

### .NET 10 Isolated 固有の注意事項

- Azure Functions Core Tools v4 を使用してください
- `FUNCTIONS_WORKER_RUNTIME` が `dotnet-isolated` に設定されていることを確認
- Isolated worker model では、Functions ホストと .NET プロセスが分離されているため、起動時間が若干長くなる場合があります

## Batch層のテスト

### テスト項目

- **Durable Functions**: オーケストレーション機能のテスト
- **Rakuten API Integration**: 楽天APIからのデータ取得
- **Blob Storage**: 画像の保存と取得
- **Database Integration**: PostgreSQL へのデータ保存

### ローカル実行

```bash
# Azurite を起動（別のターミナル）
azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log

# Azure Functions を起動
cd src/ComiCal.Server/ComiCal.Batch
func start

# 別のターミナルでテスト実行
cd scripts
.\test-integration.ps1 -Environment Local -TestBatch
```

### Durable Functions の注意事項

- `AzureWebJobsStorage` は接続文字列形式を維持する必要があります（Durable Functions の互換性のため）
- オーケストレーションの状態は Azure Storage（またはAzurite）に保存されます

## フロントエンドのテスト

### テスト項目

- **E2E Tests**: Angular アプリケーションのエンドツーエンドテスト
- **Unit Tests**: コンポーネントとサービスのユニットテスト

### ローカル実行

```bash
# フロントエンドを起動
cd src/front
npm run start

# ユニットテストを実行
npm run test

# E2Eテストを実行
npm run e2e
```

## トラブルシューティング

### PostgreSQL 接続エラー

```
Npgsql.NpgsqlException: Connection refused
```

**解決方法**:
- PostgreSQL が起動しているか確認（`docker ps | grep postgres`）
- 接続文字列が正しいか確認
- ポート 5432 が使用可能か確認

### Azure Functions 起動エラー

```
Failed to start Azure Functions runtime
```

**解決方法**:
- Azure Functions Core Tools のバージョンを確認（`func --version` で 4.x.x であることを確認）
- `FUNCTIONS_WORKER_RUNTIME` が `dotnet-isolated` に設定されているか確認
- `local.settings.json` が正しく設定されているか確認
- .NET 10 SDK がインストールされているか確認（`dotnet --version`）

### Azurite 接続エラー

```
Unable to connect to Azure Storage
```

**解決方法**:
- Azurite が起動しているか確認
- StorageConnectionString が正しく設定されているか確認
- ポート 10000, 10001, 10002 が使用可能か確認

### テストがタイムアウトする

**解決方法**:
- `ResponseTimeThresholdMs` パラメータを調整
- ネットワーク接続を確認
- Azure リソースの状態を確認（Dev/Staging/Prod 環境）

## CI/CD パイプラインでのテスト

GitHub Actions や Azure DevOps での自動テスト実行については、以下の設定を確認してください：

1. **.NET 10 SDK のインストール**
   ```yaml
   - uses: actions/setup-dotnet@v3
     with:
       dotnet-version: '10.0.x'
   ```

2. **Azure Functions Core Tools v4 のインストール**
   ```yaml
   - run: npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

3. **環境変数の設定**
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `PostgresConnectionString` (PostgreSQL接続文字列)
   - `StorageConnectionString` (Blob Storage接続文字列)
   - `AzureWebJobsStorage` (Durable Functions用ストレージ接続文字列)

## ベストプラクティス

1. **テスト前にサービスを起動する**: PostgreSQL、Azurite などの依存サービスをテスト前に起動してください
2. **テスト環境を分離する**: ローカル、開発、ステージング環境を適切に分離してください
3. **定期的にテストを実行する**: コードの変更後は必ず統合テストを実行してください
4. **ログを確認する**: テスト失敗時は詳細なログを確認してください
5. **.NET 10 固有の設定を確認**: Isolated worker model の設定が正しいことを確認してください

## 参考リンク

- [Azure Functions (.NET 10 Isolated) ドキュメント](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/)
- [PostgreSQL 接続](https://www.npgsql.org/doc/connection-string-parameters.html)
