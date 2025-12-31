# Storage Account Module

このモジュールは、ComiCal アプリケーション用の Azure Storage Account をデプロイし、静的ウェブサイトホスティング、Blob コンテナ、および環境別の構成を提供します。

## 概要

Storage Account Bicep モジュールは、以下の機能を提供します：

- **環境別 SKU 設定**: 開発環境と本番環境で Standard_LRS（コスト最適化）
- **Blob ストレージ**: 漫画画像用のコンテナ作成
- **CORS 設定**: フロントエンドからのアクセスを許可
- **静的ウェブサイトホスティング**: Angular フロントエンドのホスティング準備
- **セキュリティ設定**: TLS 1.2 以上、HTTPS トラフィックのみ

## 使用方法

### 基本的な使用例

```bicep
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'
    location: 'japaneast'
    projectName: 'comical'
    tags: commonTags
  }
}
```

## パラメータ

| パラメータ名 | 型 | 必須 | 説明 | デフォルト値 |
|------------|-----|------|------|-------------|
| `environmentName` | string | Yes | 環境名 (dev, prod) | - |
| `location` | string | No | Azure リージョン | resourceGroup().location |
| `projectName` | string | Yes | プロジェクト名 | - |
| `tags` | object | No | リソースタグ | {} |

## 環境別設定

### 開発環境 (dev)

- **ストレージアカウント名**: `stcomicaldjpe`
- **SKU**: Standard_LRS (ローカル冗長ストレージ)
- **アクセス層**: Hot
- **パブリックアクセス**: 有効（Blob レベル）

### 本番環境 (prod)

- **ストレージアカウント名**: `stcomicalpjpe`
- **SKU**: Standard_LRS (コスト最適化)
- **アクセス層**: Hot
- **パブリックアクセス**: 有効（Blob レベル）

## 作成されるリソース

### 1. Storage Account

- **命名規則**: `st{project}{env}{location}`
- **例**: `stcomicaldjpe` (dev), `stcomicalpjpe` (prod)
- **機能**:
  - HTTPS トラフィックのみ
  - TLS 1.2 以上
  - Azure サービスからのアクセス許可

### 2. Blob Service

- **CORS 設定**: すべてのオリジンから GET/HEAD/OPTIONS を許可
- **用途**: フロントエンドからの画像読み込み

### 3. Blob Container: images

- **パブリックアクセス**: Blob レベル
- **用途**: 漫画の表紙画像を保存
- **命名形式**: `{isbn}.{拡張子}`

## 静的ウェブサイトホスティング

静的ウェブサイトホスティングは、ストレージアカウント作成後に有効化する必要があります。

### Azure CLI で有効化

```bash
# 開発環境
az storage blob service-properties update \
  --account-name stcomicaldjpe \
  --static-website \
  --404-document index.html \
  --index-document index.html

# 本番環境
az storage blob service-properties update \
  --account-name stcomicalpjpe \
  --static-website \
  --404-document index.html \
  --index-document index.html
```

### Azure Portal で有効化

1. Storage Account に移動
2. 左メニューから「静的 Web サイト」を選択
3. 「有効」に切り替え
4. インデックス ドキュメント名: `index.html`
5. エラー ドキュメント パス: `index.html`
6. 保存

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `storageAccountId` | string | Storage Account のリソース ID |
| `storageAccountName` | string | Storage Account 名 |
| `storageAccountPrimaryEndpoints` | object | プライマリエンドポイント（blob, web など） |
| `storageAccountBlobEndpoint` | string | Blob エンドポイント URL |
| `storageAccountWebEndpoint` | string | 静的ウェブサイトエンドポイント URL |
| `imagesContainerName` | string | images コンテナ名 |
| `storageAccountConnectionStringTemplate` | string | 接続文字列テンプレート |

## Function Apps との統合

Function Apps は Managed Identity を使用して Storage Account にアクセスします。

### RBAC 権限

Security モジュールが以下の権限を自動的に付与します：

- **API Function App**: Storage Blob Data Contributor
- **Batch Function App**: Storage Blob Data Contributor

### Application Settings

Function Apps の Application Settings に以下が自動設定されます：

```
StorageAccountName=stcomicaldjpe  # または stcomicalpjpe
```

Function Apps のコード内で Managed Identity を使用してアクセス：

```csharp
var blobServiceClient = new BlobServiceClient(
    new Uri($"https://{storageAccountName}.blob.core.windows.net"),
    new DefaultAzureCredential()
);
```

## セキュリティ考慮事項

1. **パブリックアクセス**
   - images コンテナは Blob レベルでパブリックアクセスを許可
   - ストレージアカウント全体のパブリックアクセスは有効
   - 必要に応じて特定の IP からのみアクセスを制限

2. **HTTPS のみ**
   - HTTP トラフィックは許可されません
   - すべての接続は TLS 1.2 以上

3. **Managed Identity**
   - Function Apps は Managed Identity で認証
   - アクセスキーの管理が不要

## CORS 設定

フロントエンド（Angular）からの Blob 読み込みを許可するため、以下の CORS ルールが設定されています：

- **許可オリジン**: `*`（すべて）
- **許可メソッド**: GET, HEAD, OPTIONS
- **許可ヘッダー**: `*`
- **公開ヘッダー**: `*`
- **最大有効期限**: 3600 秒

本番環境では、特定のオリジン（SWA または CDN のドメイン）のみを許可するよう変更することを推奨します。

## コスト最適化

### 開発環境

- **Standard_LRS**: 最もコスト効率の良い SKU
- **Hot アクセス層**: 頻繁なアクセスに最適
- **削除ポリシー**: 不要なデータは定期的に削除

### 本番環境

- **Standard_LRS**: 小規模アプリケーション向けにコスト最適化
- **Hot アクセス層**: フロントエンドからの画像読み込み用
- **ライフサイクル管理**: 古い画像を Cool 層に移動

## トラブルシューティング

### CORS エラー

ブラウザコンソールで CORS エラーが表示される場合：

```bash
# CORS 設定を確認
az storage cors list \
  --account-name stcomicaldjpe \
  --services blob
```

### アクセス権限エラー

Function Apps からアクセスできない場合：

```bash
# RBAC 権限を確認
az role assignment list \
  --assignee <function-app-principal-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.Storage/storageAccounts/stcomicaldjpe
```

## 関連ドキュメント

- [Azure Storage Account ドキュメント](https://docs.microsoft.com/azure/storage/common/storage-account-overview)
- [静的ウェブサイトホスティング](https://docs.microsoft.com/azure/storage/blobs/storage-blob-static-website)
- [Blob Storage CORS サポート](https://docs.microsoft.com/rest/api/storageservices/cross-origin-resource-sharing--cors--support-for-the-azure-storage-services)
- [Managed Identity でストレージにアクセス](https://docs.microsoft.com/azure/storage/blobs/authorize-managed-identity)

## 次のステップ

1. 静的ウェブサイトホスティングを有効化
2. Angular ビルド成果物を `$web` コンテナにデプロイ
3. 本番環境で CDN を統合
4. CORS ポリシーを本番ドメインに制限

---

**最終更新日：** 2025-12-31
