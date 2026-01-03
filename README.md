# まんがリマインダー(α)

展開先: https://manrem.devtakas.jp/

Angular（フロント）から Azure Functions（API）を呼び出して発売予定の漫画を検索し、
Batch（Durable Functions）が楽天ブックスAPIからデータ/画像を定期取得して PostgreSQL と Blob Storage に保存します。

## アーキテクチャ概要

```mermaid
flowchart LR
  FE((Frontend\nAngular)) -->|HTTPS| API((API\nAzure Functions))
  FE -->|画像参照| BLOB((Blob Storage\nimages))
  API --> PG((PostgreSQL))
  BATCH((Batch\nDurable Functions)) --> RAKUTEN((Rakuten Books API))
  BATCH --> PG
  BATCH --> BLOB
```

**主要技術スタック**:
- **フロントエンド**: Angular 21, Azure Static Web Apps
- **API**: Azure Functions (.NET 10 + Isolated worker model), PostgreSQL
- **Batch**: Azure Durable Functions (.NET 10 + Isolated worker model), Blob Storage
- **外部API**: 楽天ブックスAPI
- **開発環境**: Dev Container with Docker Compose

**技術詳細**:
- フロントエンド: Angular 21
- API/Batch: Azure Functions v4 / dotnet-isolated（プロジェクトは net10.0）
- DB: PostgreSQL（Dev Container は `postgres:15-alpine`）
- ストレージ: Azure Blob Storage（ローカルは Azurite）
- 外部API: 楽天ブックスAPI
- .NET 10: DevContainerでビルド時に自動インストール

**注意**: .NET 10は.NET 8ベースイメージにビルド時に追加インストールされます。

## ディレクトリ構成

- `src/front`: フロントエンド（Angular）
- `src/ComiCal.Server/Comical.Api`: API層（Functions）
- `src/ComiCal.Server/ComiCal.Batch`: Batch層（Durable Functions）
- `src/ComiCal.Server/ComiCal.Shared`: API/Batch 共有の設定・モデル等
- `src/api/Comical.Api`: API層の別バージョン（レガシー？）
- `src/database`: PostgreSQL 初期化SQL、スキーマ説明、シード
- `scripts`: 補助スクリプト

## ローカル開発（Dev Container 推奨）

### 1) Dev Container で起動

Dev Container を開くと、以下が自動セットアップされます：

**サービス**（同一 compose ネットワーク上）:
- PostgreSQL: `postgres:5432`（DB: `comical` / user: `comical`）
- Azurite: `azurite:10000`（Blob）/ `10001`（Queue）/ `10002`（Table）

**自動セットアップ**（postCreateCommand）:
1. PostgreSQL・Azuriteの起動待機
2. Node.js依存関係のインストール（フロント・データベース）
3. データベーススキーマ・シードデータの投入
4. Azuriteへの画像データシード

**動作確認用スクリプト**:

```bash
./test-devcontainer.sh  # PostgreSQL移行テスト
./test-services.sh     # サービス接続テスト
```

### 2) 設定ファイル（Functions）

テンプレートをコピーして利用します（Dev Container のサービス名を前提に設定済み）。

```bash
cp src/ComiCal.Server/Comical.Api/local.settings.json.template src/ComiCal.Server/Comical.Api/local.settings.json
```

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PostgresConnectionString": "Host=postgres;Port=5432;Database=comical;Username=comical;Password=comical_dev_password",
    "StorageConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;..."
  }
}
```

**Batch層の設定** (`src/ComiCal.Server/ComiCal.Batch/local.settings.json`):

```bash
# テンプレートからコピー
cp src/ComiCal.Server/ComiCal.Batch/local.settings.json.template src/ComiCal.Server/ComiCal.Batch/local.settings.json
```

Batch は楽天APIの `applicationid` を必要とします（`src/ComiCal.Server/ComiCal.Batch/local.settings.json` に設定）。

### 3) 起動（API / Batch / Front）

API（既定ポート 7071）:

```bash
cd src/ComiCal.Server/Comical.Api
func start
```

Batch（APIと併走する場合はポートを変える）:

| 変数名 | 説明 | 例 |
|--------|------|-----|
| `PostgresConnectionString` | PostgreSQL 接続文字列（ローカル開発） | `Host=postgres;Port=5432;Database=comical;Username=comical;Password=comical_dev_password` |
| `PostgresConnectionString` | PostgreSQL 接続文字列（Azure with Managed Identity） | `Host=<server>.postgres.database.azure.com;Database=comical;Username=<managed-identity-name>` |
| `StorageConnectionString` | Blob Storage 接続文字列 | `DefaultEndpointsProtocol=https;AccountName=...` |
| `blobBaseUrl` | Blob Storage の画像ベースURL | `https://<account>.blob.core.windows.net/images` |

> **セキュリティ注意**: 本番環境では、接続文字列にパスワードを含めるのではなく、Azure Managed Identity を使用することを強く推奨します。これにより、設定ファイルに機密情報を保存する必要がなくなり、自動的にローテーションされる資格情報を使用できます。

#### .NET 10 Isolated移行後の設定

プロジェクトは .NET 10 と Isolated worker model に移行されています。

**.NET 10のインストール方法**:
- DevContainerでは.NET 8ベースイメージに.NET 10 SDKを**ビルド時に自動インストール**
- Dockerfileで`dotnet-install.sh`スクリプトを使用してチャンネル10.0をインストール

以下の設定手順に従ってください：

**手順**:

1. **local.settings.jsonを作成** (テンプレートからコピー)

```bash
# API層
cp src/ComiCal.Server/Comical.Api/local.settings.json.template src/ComiCal.Server/Comical.Api/local.settings.json

# Batch層
cp src/ComiCal.Server/ComiCal.Batch/local.settings.json.template src/ComiCal.Server/ComiCal.Batch/local.settings.json
```

2. **FUNCTIONS_WORKER_RUNTIMEを`dotnet-isolated`に設定**
   
local.settings.json内で以下を確認してください：

```json
"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
```

> **重要**: .NET 10 Isolated worker modelを使用するため、`dotnet-isolated`の設定が必須です。

3. **ローカル開発: StorageConnectionStringのみ設定 (Azurite)**
   
ローカル開発環境では、DevContainerのAzuriteを使用します：

```json
"StorageConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;"
```

> **注**: DevContainerではサービス名`azurite`を使用し、外部アクセス時は`localhost`を使用します。

4. **Azure環境: StorageAccountNameを追加してManaged Identity有効化**
   
Azure環境では、Managed Identityを使用することを推奨します：
- `StorageAccountName` を設定してManaged Identity認証を有効化
- `StorageConnectionString` はフォールバック用に保持

Application Settingsでの設定例：

```
StorageAccountName=<your-storage-account-name>
StorageConnectionString=DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;...
```

5. **AzureWebJobsStorageは接続文字列形式を継続**
   
Durable Functions互換性のため、`AzureWebJobsStorage`は接続文字列形式を維持します：
```json
"AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;..."
```

6. **Azure Functions Core Tools v4を使用**
   
開発には Azure Functions Core Tools v4 を使用してください：

```bash
# バージョン確認
func --version  # 4.x.x であることを確認

# インストール（必要な場合）
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

#### 4. ローカル開発実行

**前提条件**:
- DevContainerが起動されていること
- postCreateCommandによる自動セットアップが完了していること
  - PostgreSQL（docker: `comical-postgres`）
  - Azurite（docker: `comical-azurite`）

**実行手順**:
1. apiデバッグ実行/apiディレクトリで`func start`
2. frontディレクトリで`npm run start`
3. frontディレクトリで`npm run start:swa`
4. http://localhost:4280

> **ポート情報**: DevContainerでは4200, 5432, 10000, 10001, 10002がホストに自動フォワードされます。

## 統合テストとデプロイ

### 統合テストの実行

詳細な統合テスト手順は [統合テストガイド](./docs/INTEGRATION_TESTS.md) を参照してください。

**PostgreSQL移行テスト**:
```bash
# DevContainer PostgreSQL移行のテスト
./test-devcontainer.sh

# サービス接続テスト（PostgreSQL, Azurite）
./test-services.sh
```

**クイックスタート**:
```powershell
# すべてのテストを実行（ローカル環境）
cd scripts
.\test-integration.ps1 -Environment Local -RunAllTests

# API層のテストのみ実行
.\test-integration.ps1 -Environment Local -TestApi

# 開発環境でテスト実行
.\test-integration.ps1 -Environment Dev -RunAllTests
```

Front（APIは `proxy.conf.json` で `http://localhost:7071` に転送）:

```bash
cd src/front
npm install
npm run start
```

SWAローカル（必要な場合のみ）:

```bash
cd src/front
npm run start:swa
```

### 4) 画像表示（ローカル）

ローカルの画像URLは `src/front/src/environments/environment.ts` の `blobBaseUrl` で決まります。

DevContainer + Azuriteの設定:
- **コンテナ内**: `http://azurite:10000/devstoreaccount1/images`
- **ホストから**: `http://localhost:10000/devstoreaccount1/images`

> **注意**: DevContainerのpostCreateCommandで画像シードが自動実行されるため、起動直後から画像が表示されます。

## ドキュメント

コードから判明した内容を /docs 配下に大項目ごとに最小限でまとめています。

- [docs/index.md](./docs/index.md)
- [docs/architecture.md](./docs/architecture.md)
- [docs/api.md](./docs/api.md)
- [docs/batch.md](./docs/batch.md)
- [docs/frontend.md](./docs/frontend.md)
- [docs/database.md](./docs/database.md)
- [docs/shared.md](./docs/shared.md)

DBスキーマの詳細: [database/SCHEMA.md](./database/SCHEMA.md)

## コンポーネント・設定ドキュメント

### メインコンポーネント
- **API**: [src/ComiCal.Server/Comical.Api](./src/ComiCal.Server/Comical.Api) - Azure Functions API層
- **Batch**: [src/ComiCal.Server/ComiCal.Batch](./src/ComiCal.Server/ComiCal.Batch) - Durable Functions バッチ層
- **Front**: [src/front/README.md](./src/front/README.md) - Angular フロントエンド
- **Database**: [database/README.md](./database/README.md) - PostgreSQL データベース設定

### 設定・スクリプト
- **Scripts**: [scripts/README.md](./scripts/README.md) - セットアップスクリプト
- **DevContainer**: [.devcontainer/](./.devcontainer/) - Docker Compose開発環境設定

DBスキーマの詳細: [database/SCHEMA.md](./database/SCHEMA.md)

## ライセンス

このプロジェクトは個人プロジェクトです。