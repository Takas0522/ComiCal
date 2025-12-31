# Static Web Apps Module

このモジュールは、Azure Static Web Apps をデプロイし、GitHub リポジトリとの自動連携、環境別構成、PR 環境の無効化を提供します。

## 概要

Static Web Apps Bicep モジュールは、以下の機能を提供します：

- **GitHub Repository 自動連携**: GitHub リポジトリとの自動デプロイ設定
- **環境別 SWA**: dev・prod 独立した Static Web Apps (stapp-comical-dev/prod)
- **PR 環境無効化**: staging_environment_policy を Disabled に設定
- **Angular ビルド統合**: Angular プロジェクトの自動ビルド・デプロイ
- **API バックエンド接続**: Function Apps/Container Apps への自動接続
- **カスタムドメイン対応**: カスタムドメイン設定が可能

## 使用方法

### 基本的な使用例

```bicep
module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'
    location: 'japaneast'
    projectName: 'comical'
    repositoryUrl: 'https://github.com/Takas0522/ComiCal'
    repositoryBranch: 'main'
    repositoryToken: githubToken
    apiBackendUrl: 'https://ca-comical-api-dev-jpe.azurecontainerapps.io'
    sku: 'Free'
    tags: commonTags
  }
}
```

### GitHub Actions での使用例

```yaml
- name: Deploy Infrastructure
  uses: azure/arm-deploy@v1
  with:
    template: ./infra/main.bicep
    parameters: |
      environmentName=dev
      githubToken=${{ secrets.GITHUB_TOKEN }}
      repositoryUrl=https://github.com/${{ github.repository }}
      repositoryBranch=${{ github.ref_name }}
```

## パラメータ

| パラメータ名 | 型 | 必須 | 説明 | デフォルト値 |
|------------|-----|------|------|-------------|
| `environmentName` | string | Yes | 環境名 (dev, prod) | - |
| `location` | string | No | Azure リージョン | resourceGroup().location |
| `projectName` | string | Yes | プロジェクト名 | - |
| `repositoryUrl` | string | No | GitHub リポジトリ URL | 'https://github.com/Takas0522/ComiCal' |
| `repositoryBranch` | string | No | GitHub リポジトリブランチ | 'main' |
| `repositoryToken` | string (secure) | No | GitHub 認証トークン | '' |
| `apiBackendUrl` | string | Yes | API バックエンド URL (Container Apps/Functions) | - |
| `sku` | string | No | Static Web Apps SKU (Free, Standard) | 'Free' |
| `tags` | object | No | リソースタグ | {} |

## 環境別設定

### 開発環境 (dev)

- **SKU**: Free
  - 無料プランで十分な機能を提供
  - カスタムドメイン制限あり
- **PR 環境**: 無効（staging_environment_policy: Disabled）
- **Angular ビルド**: development 構成
- **API 接続**: dev Container Apps に接続

### 本番環境 (prod)

- **SKU**: Standard
  - カスタムドメイン対応
  - SLA 保証
- **PR 環境**: 無効（staging_environment_policy: Disabled）
- **Angular ビルド**: production 構成
- **API 接続**: prod Container Apps に接続

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `staticWebAppId` | string | Static Web App のリソース ID |
| `staticWebAppName` | string | Static Web App 名 |
| `staticWebAppDefaultHostname` | string | Static Web App のデフォルトホスト名 |
| `staticWebAppApiKey` | string | Static Web App の API キー（デプロイ用） |
| `staticWebAppRepositoryUrl` | string | 接続された GitHub リポジトリ URL |
| `staticWebAppBranch` | string | 接続されたブランチ名 |
| `stagingEnvironmentPolicy` | string | PR 環境ポリシー (Disabled) |

## 構成ファイル

### staticwebapp.config.json

Static Web Apps の動作を制御する設定ファイル：

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/api/*"]
  },
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "X-XSS-Protection": "1; mode=block"
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  }
}
```

### ビルド設定

Angular プロジェクトのビルド設定：

- **アプリケーション場所**: `src/front`
- **API 場所**: (空) - Container Apps を使用
- **出力場所**: `dist/front/browser`
- **ビルドコマンド**: `npm run build`

## GitHub Repository との自動連携

### 自動生成されるワークフロー

Static Web Apps をデプロイすると、GitHub Actions ワークフローが自動生成されます：

- **ワークフロー名**: `Azure Static Web Apps CI/CD`
- **トリガー**: 
  - `push` to `main` branch
  - `pull_request` to `main` branch
- **デプロイ先**: 
  - dev: `stapp-comical-dev-jpe`
  - prod: `stapp-comical-prod-jpe`

### ワークフローのカスタマイズ

自動生成されたワークフローは、必要に応じてカスタマイズできます：

```yaml
- name: Build And Deploy
  uses: Azure/static-web-apps-deploy@v1
  with:
    app_location: "src/front"
    output_location: "dist/front/browser"
    app_build_command: "npm run build"
```

## API バックエンド接続

### Container Apps との接続

Static Web Apps は、環境別の Container Apps に自動接続されます：

- **開発環境**: `https://ca-comical-api-dev-jpe.azurecontainerapps.io`
- **本番環境**: `https://ca-comical-api-prod-jpe.azurecontainerapps.io`

### API プロキシ設定

`staticwebapp.config.json` で API ルーティングを設定：

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ]
}
```

## PR 環境の無効化

### staging_environment_policy: Disabled

PR 環境を無効化することで、以下のメリットがあります：

1. **コスト削減**: PR ごとの環境作成を防止
2. **セキュリティ**: 不要な環境へのアクセスを制限
3. **リソース管理**: 環境の数を制御

### 有効化方法（必要な場合）

PR 環境を有効化するには、パラメータを変更：

```bicep
properties: {
  stagingEnvironmentPolicy: 'Enabled'
}
```

## カスタムドメイン設定

### Standard SKU での設定

Standard SKU では、カスタムドメインの設定が可能です：

```bash
# Azure CLI でカスタムドメインを追加
az staticwebapp hostname set \
  --name stapp-comical-prod-jpe \
  --resource-group rg-comical-p-jpe \
  --hostname www.example.com
```

### DNS 設定

カスタムドメインには、CNAME レコードを設定：

```
CNAME www.example.com -> stapp-comical-prod-jpe.azurestaticapps.net
```

## セキュリティ考慮事項

1. **GitHub Token 管理**
   - `repositoryToken` は GitHub Secrets で管理
   - PAT (Personal Access Token) または GitHub App を使用
   - 必要最小限の権限を付与

2. **API キー保護**
   - Static Web Apps API キーは機密情報
   - GitHub Secrets で管理
   - デプロイワークフローでのみ使用

3. **セキュリティヘッダー**
   - `staticwebapp.config.json` でセキュリティヘッダーを設定
   - XSS, Clickjacking などの攻撃を防止

## コスト最適化

### 開発環境のコスト削減

1. **Free SKU の使用**
   - 開発環境では Free SKU で十分
   - 無料で利用可能

2. **PR 環境の無効化**
   - PR ごとの環境作成を防止
   - コストとリソースを節約

### 本番環境のコスト管理

1. **Standard SKU の適切な使用**
   - カスタムドメインが必要な場合のみ Standard SKU を使用
   - SLA 保証とサポート

## トラブルシューティング

### デプロイエラー

1. **GitHub Token の確認**
   ```bash
   # Token の権限を確認
   curl -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/user
   ```

2. **ワークフロー生成の確認**
   - `.github/workflows/` にワークフローが生成されているか確認
   - API キーが正しく設定されているか確認

### ビルドエラー

1. **Angular ビルド設定の確認**
   ```bash
   # ローカルでビルドを実行
   cd src/front
   npm install
   npm run build
   ```

2. **出力パスの確認**
   - `angular.json` の `outputPath` が `dist/front/browser` になっているか確認

### API 接続エラー

1. **バックエンド URL の確認**
   ```bash
   # Container Apps の URL を確認
   az containerapp show \
     --name ca-comical-api-dev-jpe \
     --resource-group rg-comical-d-jpe \
     --query properties.configuration.ingress.fqdn
   ```

2. **CORS 設定の確認**
   - Container Apps で SWA のドメインを許可しているか確認

## 関連ドキュメント

- [Azure Static Web Apps ドキュメント](https://docs.microsoft.com/azure/static-web-apps/)
- [GitHub Actions 統合](https://docs.microsoft.com/azure/static-web-apps/github-actions-workflow)
- [staticwebapp.config.json リファレンス](https://docs.microsoft.com/azure/static-web-apps/configuration)
- [カスタムドメイン設定](https://docs.microsoft.com/azure/static-web-apps/custom-domain)

## 変更履歴

- **2025-12-31**: 初期リリース
  - GitHub Repository 自動連携
  - 環境別 SWA (dev/prod)
  - PR 環境無効化
  - Angular ビルド統合
  - Container Apps 接続
