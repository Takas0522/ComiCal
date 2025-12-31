# CDN Module

このモジュールは、本番環境の Azure CDN（Content Delivery Network）をデプロイし、Storage Account の静的ウェブサイトホスティングと統合します。フロントエンド（Angular）の高速配信と低レイテンシを実現します。

## 概要

CDN Bicep モジュールは、以下の機能を提供します：

- **本番環境専用**: prod 環境でのみデプロイ
- **Standard Microsoft SKU**: コスト効率の良い CDN プロファイル
- **圧縮有効**: HTML、CSS、JavaScript などの自動圧縮
- **Storage 統合**: Storage Account の静的ウェブサイトをオリジンとして使用
- **グローバル配信**: 世界中のエッジロケーションから配信

## 使用方法

### 基本的な使用例

```bicep
module cdn 'modules/cdn.bicep' = {
  name: 'cdn-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'prod'  // 'dev' の場合はデプロイされません
    location: 'japaneast'
    projectName: 'comical'
    storageWebEndpoint: storage.outputs.storageAccountWebEndpoint
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
| `storageWebEndpoint` | string | Yes | Storage Account の静的ウェブサイトエンドポイント | - |
| `tags` | object | No | リソースタグ | {} |

## 環境別動作

### 開発環境 (dev)

- **CDN Profile**: 作成されません
- **CDN Endpoint**: 作成されません
- **理由**: 開発環境では Storage の静的ウェブサイトに直接アクセス

### 本番環境 (prod)

- **CDN Profile**: 作成される
- **CDN Endpoint**: 作成される
- **理由**: 本番環境では高速配信と低レイテンシが必要

## 作成されるリソース

### 1. CDN Profile

- **命名規則**: `cdn-{project}-{env}`
- **例**: `cdn-comical-prod`
- **SKU**: Standard_Microsoft
- **ロケーション**: Global（CDN はグローバルサービス）

### 2. CDN Endpoint

- **命名規則**: `cdn-{project}-{env}-{location}`
- **例**: `cdn-comical-prod-jpe`
- **機能**:
  - HTTP/HTTPS サポート
  - クエリ文字列キャッシング: 無視
  - コンテンツ圧縮: 有効

## CDN 設定詳細

### オリジン設定

- **オリジンホスト名**: Storage Account の静的ウェブサイトエンドポイント
  - 例: `stcomicalpjpe.z11.web.core.windows.net`
- **オリジンホストヘッダー**: オリジンホスト名と同じ
- **プロトコル**: HTTP (80)、HTTPS (443)

### キャッシング

- **クエリ文字列キャッシング**: `IgnoreQueryString`
  - クエリパラメータを無視してキャッシュ
  - Angular の URL ルーティングに最適

### 圧縮

以下のコンテンツタイプが自動的に圧縮されます：

- **HTML/XML**: text/html, application/xhtml+xml, application/xml
- **CSS**: text/css
- **JavaScript**: application/javascript, text/javascript, application/x-javascript
- **JSON**: application/json
- **フォント**: application/font-*, font/*
- **SVG**: image/svg+xml

## CDN エンドポイント URL

CDN エンドポイントの URL 形式：

```
https://cdn-comical-prod-jpe.azureedge.net/
```

この URL をフロントエンドアプリケーションのベース URL として使用します。

## Angular での使用

### environment.prod.ts

```typescript
export const environment = {
  production: true,
  apiBaseUrl: 'https://func-comical-api-prod-jpe.azurewebsites.net',
  blobBaseUrl: 'https://cdn-comical-prod-jpe.azureedge.net/',  // CDN URL
  staticWebUrl: 'https://cdn-comical-prod-jpe.azureedge.net/'  // CDN URL
};
```

## カスタムドメインの設定

### 1. CDN エンドポイントにカスタムドメインを追加

```bash
# カスタムドメインを追加
az cdn custom-domain create \
  --resource-group rg-comical-p-jpe \
  --profile-name cdn-comical-prod \
  --endpoint-name cdn-comical-prod-jpe \
  --name comical-custom-domain \
  --hostname www.example.com
```

### 2. DNS レコードの設定

DNS プロバイダーで CNAME レコードを追加：

```
www.example.com -> cdn-comical-prod-jpe.azureedge.net
```

### 3. HTTPS の有効化

```bash
# Managed Certificate で HTTPS を有効化
az cdn custom-domain enable-https \
  --resource-group rg-comical-p-jpe \
  --profile-name cdn-comical-prod \
  --endpoint-name cdn-comical-prod-jpe \
  --name comical-custom-domain \
  --min-tls-version 1.2
```

## キャッシュのパージ

コンテンツを更新した後、CDN キャッシュをパージする必要があります：

```bash
# すべてのキャッシュをパージ
az cdn endpoint purge \
  --resource-group rg-comical-p-jpe \
  --profile-name cdn-comical-prod \
  --name cdn-comical-prod-jpe \
  --content-paths '/*'

# 特定のファイルをパージ
az cdn endpoint purge \
  --resource-group rg-comical-p-jpe \
  --profile-name cdn-comical-prod \
  --name cdn-comical-prod-jpe \
  --content-paths '/index.html' '/main.*.js'
```

## CI/CD 統合

GitHub Actions でのキャッシュパージ例：

```yaml
- name: Purge CDN Cache
  run: |
    az cdn endpoint purge \
      --resource-group rg-comical-p-jpe \
      --profile-name cdn-comical-prod \
      --name cdn-comical-prod-jpe \
      --content-paths '/*'
```

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `cdnProfileId` | string | CDN Profile のリソース ID（prod のみ） |
| `cdnProfileName` | string | CDN Profile 名（prod のみ） |
| `cdnEndpointId` | string | CDN Endpoint のリソース ID（prod のみ） |
| `cdnEndpointName` | string | CDN Endpoint 名（prod のみ） |
| `cdnEndpointHostname` | string | CDN Endpoint のホスト名（prod のみ） |
| `cdnEnabled` | bool | CDN が有効かどうか |

## パフォーマンス最適化

### 1. キャッシュ戦略

- **静的アセット**: 長期キャッシュ（1 年）
  - CSS、JS、画像、フォント
- **HTML ファイル**: 短期キャッシュ（1 時間）
  - index.html など

### 2. 圧縮

CDN が自動的にコンテンツを圧縮するため、帯域幅を削減：

- **HTML**: 約 70% 圧縮
- **CSS**: 約 80% 圧縮
- **JavaScript**: 約 70% 圧縮

### 3. エッジキャッシング

世界中のエッジロケーションからコンテンツを配信：

- **日本**: 東京、大阪
- **アジア**: 香港、シンガポール、ソウル
- **その他**: 北米、ヨーロッパ、オーストラリアなど

## セキュリティ考慮事項

1. **HTTPS**
   - 本番環境では HTTPS を強制
   - TLS 1.2 以上

2. **オリジンのアクセス制御**
   - Storage Account のパブリックアクセスは必要
   - CDN のみがアクセスするよう制限可能（Private Link）

3. **カスタムドメイン**
   - Managed Certificate で自動更新
   - Let's Encrypt 証明書を使用

## コスト

### Standard Microsoft SKU

- **データ転送（アウトバウンド）**:
  - 最初の 10 TB/月: 約 ¥9.50/GB
  - 10-50 TB/月: 約 ¥8.50/GB
  - 50+ TB/月: 約 ¥7.50/GB
- **HTTP/HTTPS リクエスト**: 約 ¥0.01/10,000 リクエスト

### コスト削減効果

- **帯域幅削減**: 圧縮により約 70% 削減
- **Origin 負荷削減**: キャッシュにより Storage へのアクセス削減
- **グローバル配信**: エッジキャッシングで低レイテンシ

## モニタリング

### メトリクスの確認

```bash
# CDN エンドポイントのメトリクスを表示
az monitor metrics list \
  --resource <cdn-endpoint-id> \
  --metric-names TotalLatency BytesSentToClient RequestCount
```

### Azure Portal でモニタリング

1. Azure Portal で CDN Endpoint に移動
2. 左メニューから「メトリック」を選択
3. 以下のメトリクスを確認：
   - **リクエスト数**: トラフィック量
   - **帯域幅**: データ転送量
   - **レイテンシ**: 応答時間
   - **キャッシュヒット率**: キャッシュ効率

## トラブルシューティング

### コンテンツが更新されない

キャッシュをパージ：

```bash
az cdn endpoint purge \
  --resource-group rg-comical-p-jpe \
  --profile-name cdn-comical-prod \
  --name cdn-comical-prod-jpe \
  --content-paths '/*'
```

### 404 エラー

オリジンの静的ウェブサイトが有効か確認：

```bash
# Storage Account の静的ウェブサイト設定を確認
az storage blob service-properties show \
  --account-name stcomicalpjpe \
  --query 'staticWebsite'
```

### カスタムドメインが機能しない

DNS レコードが正しく設定されているか確認：

```bash
# CNAME レコードを確認
nslookup www.example.com

# 期待される結果
# www.example.com -> cdn-comical-prod-jpe.azureedge.net
```

## 関連ドキュメント

- [Azure CDN ドキュメント](https://docs.microsoft.com/azure/cdn/)
- [CDN でのコンテンツ圧縮](https://docs.microsoft.com/azure/cdn/cdn-improve-performance)
- [CDN カスタムドメイン](https://docs.microsoft.com/azure/cdn/cdn-map-content-to-custom-domain)
- [CDN キャッシュ動作](https://docs.microsoft.com/azure/cdn/cdn-caching-rules)

## 次のステップ

1. カスタムドメインを設定
2. HTTPS を有効化
3. キャッシュ戦略を最適化
4. Azure Monitor でパフォーマンスを監視

---

**最終更新日：** 2025-12-31
