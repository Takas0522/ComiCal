# Database Module

このモジュールは、PostgreSQL Flexible Server をデプロイし、環境別の構成とコスト最適化を提供します。

## 概要

PostgreSQL Flexible Server Bicep モジュールは、以下の機能を提供します：

- **環境別 SKU 設定**: 開発環境では Burstable SKU、本番環境では General Purpose SKU
- **Azure AD 認証**: Managed Identity による安全な認証
- **ファイアウォール設定**: Azure サービスからのアクセスを許可
- **自動スケーリング**: ストレージの自動拡張
- **バックアップ設定**: 環境別のバックアップ保持期間

## 使用方法

### 基本的な使用例

```bicep
module database 'modules/database.bicep' = {
  name: 'database-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'
    location: 'japaneast'
    projectName: 'comical'
    administratorLogin: 'psqladmin'
    administratorLoginPassword: 'SecurePassword123!'
    tags: commonTags
  }
}
```

### Azure AD 管理者の設定

```bicep
module database 'modules/database.bicep' = {
  name: 'database-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'prod'
    location: 'japaneast'
    projectName: 'comical'
    administratorLogin: 'psqladmin'
    administratorLoginPassword: 'SecurePassword123!'
    aadAdminObjectId: '00000000-0000-0000-0000-000000000000'
    aadAdminPrincipalName: 'admin@example.com'
    aadAdminPrincipalType: 'User'
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
| `administratorLogin` | string (secure) | Yes | PostgreSQL 管理者ユーザー名 | - |
| `administratorLoginPassword` | string (secure) | Yes | PostgreSQL 管理者パスワード | - |
| `aadAdminObjectId` | string | No | Azure AD 管理者のオブジェクト ID | '' |
| `aadAdminPrincipalName` | string | No | Azure AD 管理者のプリンシパル名 | '' |
| `aadAdminPrincipalType` | string | No | Azure AD 管理者のタイプ | 'User' |
| `tags` | object | No | リソースタグ | {} |

## 環境別設定

### 開発環境 (dev)

- **SKU**: Standard_B2s (Burstable)
  - コスト最適化されたバースト可能な SKU
  - 低トラフィックの開発環境に最適
- **ストレージ**: 32 GB
- **バックアップ保持期間**: 7 日
- **Geo 冗長バックアップ**: 無効
- **高可用性**: 無効
- **可用性ゾーン**: なし

### 本番環境 (prod)

- **SKU**: Standard_D2s_v3 (General Purpose)
  - 本番ワークロード向けの汎用 SKU
  - 一貫したパフォーマンス
- **ストレージ**: 128 GB
- **バックアップ保持期間**: 30 日
- **Geo 冗長バックアップ**: 有効
- **高可用性**: ゾーン冗長
- **可用性ゾーン**: ゾーン 1 (スタンバイ: ゾーン 2)

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `postgresServerName` | string | PostgreSQL サーバー名 |
| `postgresServerFqdn` | string | PostgreSQL サーバーの完全修飾ドメイン名 (FQDN) |
| `databaseName` | string | データベース名 |
| `postgresServerId` | string | PostgreSQL サーバーのリソース ID |
| `connectionStringTemplate` | string | 接続文字列テンプレート |
| `skuName` | string | 適用された SKU 名 |
| `skuTier` | string | 適用された SKU ティア |

## セキュリティ考慮事項

1. **パスワード管理**
   - `administratorLoginPassword` は必ず安全な方法で提供してください
   - GitHub Secrets または Azure Key Vault の使用を推奨

2. **Azure AD 認証**
   - 本番環境では Azure AD 認証を有効化することを推奨
   - Managed Identity を使用してアプリケーションから接続

3. **ファイアウォール**
   - デフォルトでは Azure サービスからのアクセスのみ許可
   - 必要に応じて追加のファイアウォールルールを設定

## コスト最適化

### 開発環境のコスト削減

1. **Burstable SKU の使用**
   - Standard_B2s は開発環境に最適
   - アイドル時のコストが低い

2. **ストレージの最小化**
   - 開発環境では 32 GB から開始
   - 必要に応じて自動拡張

3. **バックアップ設定**
   - 7 日間の保持期間
   - Geo 冗長バックアップなし

### 本番環境のコスト管理

1. **適切な SKU 選択**
   - General Purpose SKU で一貫したパフォーマンス
   - 必要に応じてスケールアップ

2. **高可用性**
   - ゾーン冗長で障害に強い構成
   - ダウンタイムの最小化

## Managed Identity の設定

Managed Identity を使用して Function Apps から PostgreSQL に接続するには、`setup-postgres-identity.sh` スクリプトを使用してください。

```bash
./infra/scripts/setup-postgres-identity.sh dev rg-comical-d-jpe
```

詳細は [setup-postgres-identity.sh のドキュメント](../scripts/README.md) を参照してください。

## トラブルシューティング

### 接続エラー

1. **ファイアウォール設定の確認**
   ```bash
   az postgres flexible-server firewall-rule list \
     --resource-group <resource-group> \
     --name <server-name>
   ```

2. **Azure AD 認証の確認**
   ```bash
   az postgres flexible-server ad-admin list \
     --resource-group <resource-group> \
     --server-name <server-name>
   ```

### SKU の変更

開発環境と本番環境で異なる SKU が必要な場合、モジュールは自動的に環境に応じた SKU を適用します。

## 関連ドキュメント

- [PostgreSQL Flexible Server ドキュメント](https://docs.microsoft.com/azure/postgresql/flexible-server/)
- [Azure AD 認証](https://docs.microsoft.com/azure/postgresql/flexible-server/how-to-configure-sign-in-aad-authentication)
- [Managed Identity](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)

## 変更履歴

- **2025-12-31**: 初期リリース
  - 環境別 SKU 設定
  - Azure AD 認証サポート
  - コスト最適化設定
