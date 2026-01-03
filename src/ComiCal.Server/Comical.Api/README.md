# ComiCal API (Azure Functions)

Azure Functions を使用した API層。.NET 10 + Isolated worker model で実装されています。

## 構成

- **実行環境**: Azure Functions v4, .NET 10 Isolated worker model
- **データベース**: PostgreSQL (Entity Framework Core)
- **認証**: Azure Functions の認証レベル設定
- **DI コンテナ**: Microsoft.Extensions.DependencyInjection

## エンドポイント

### `POST /api/ComicData`

漫画データの検索API

**リクエスト**:
- メソッド: POST
- クエリパラメータ:
  - `fromdate` (任意): 発売日の開始日（YYYY-MM-DD形式）
- ボディ: `GetComicsRequest`
  ```json
  {
    "searchList": ["検索キーワード1", "検索キーワード2"]
  }
  ```

**レスポンス**:
```json
[
  {
    "isbn": "9784123456789",
    "title": "漫画タイトル",
    "author": "著者名",
    "salesdate": "2024-01-15",
    "imageurl": "https://example.com/image.jpg"
  }
]
```

### `GET /api/ConfigMigration`

設定マイグレーション用のエンドポイント（開発・デバッグ用）

## ローカル開発

### 前提条件

- .NET 10 SDK
- Azure Functions Core Tools v4
- PostgreSQL (DevContainerで提供)

### 設定

1. **設定ファイルの作成**:
```bash
cp local.settings.json.template local.settings.json
```

2. **local.settings.json の設定**:
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

### 起動

```bash
func start
```

- デフォルトポート: 7071
- 他のサービスとポートが競合する場合は `func start --port 7072` で変更可能

## 本番環境

### Azure環境での設定

Application Settings で以下を設定：

- `FUNCTIONS_WORKER_RUNTIME`: `dotnet-isolated`
- `PostgresConnectionString`: Managed Identity使用時はパスワード不要
- `StorageAccountName`: Managed Identity認証用
- `StorageConnectionString`: フォールバック用

### セキュリティ

- Managed Identity を使用してデータベース・ストレージアクセス
- 接続文字列にパスワードを含めない設定を推奨
- HTTPS強制、CORS設定を適切に構成

## プロジェクト構造

```
Comical.Api/
├── Functions/           # Azure Functions エンドポイント
├── Models/             # データモデル・DTOクラス  
├── Services/           # ビジネスロジック層
├── Repositories/       # データアクセス層
├── Program.cs          # DI設定・アプリケーション構成
├── host.json          # Functions ホスト設定
└── local.settings.json # ローカル環境設定
```

## 依存関係

主要なNuGetパッケージ：
- `Microsoft.Azure.Functions.Worker`
- `Microsoft.Azure.Functions.Worker.Sdk`
- `Microsoft.EntityFrameworkCore.Design`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Azure.Storage.Blobs`