# セキュリティ設定ガイド

## 概要

このドキュメントは、ComiCal プロジェクトにおけるセキュリティのベストプラクティスと、機密情報の適切な管理方法について説明します。

## 機密情報の管理

### 1. 環境変数

以下の機密情報は環境変数で管理し、ソースコードには直接記載しないでください：

#### フロントエンド（Angular）

| 環境変数名 | 説明 | 例 |
|------------|------|-----|
| `GOOGLE_OAUTH_CLIENT_ID` | Google OAuth Client ID | `123456789-xxx.apps.googleusercontent.com` |
| `BLOB_BASE_URL` | 画像ストレージのベースURL | `https://storage.blob.core.windows.net/images` |

#### バックエンド（Azure Functions）

| 環境変数名 | 説明 | Azure での設定方法 |
|------------|------|-------------------|
| `DefaultConnection` | PostgreSQL接続文字列 | Function App > 構成 > 接続文字列 |
| `AzureWebJobsStorage` | ストレージアカウント接続文字列 | Function App > 構成 > アプリケーション設定 |
| `RAKUTEN_APP_ID` | 楽天ブックスAPI ApplicationID | Function App > 構成 > アプリケーション設定 |

### 2. Azure Key Vault 統合

本番環境では、以下の設定でKey Vault参照を使用することを推奨：

```bash
# Key Vault参照の例
AzureWebJobsStorage="@Microsoft.KeyVault(SecretUri=https://vault-name.vault.azure.net/secrets/AzureWebJobsStorage/)"
DefaultConnection="@Microsoft.KeyVault(SecretUri=https://vault-name.vault.azure.net/secrets/PostgresConnection/)"
RAKUTEN_APP_ID="@Microsoft.KeyVault(SecretUri=https://vault-name.vault.azure.net/secrets/RakutenAppId/)"
```

## セキュリティチェックリスト

### ✅ 実装済み

- [x] `local.settings.json`が.gitignoreに含まれている
- [x] テンプレートファイルには実際の機密情報が含まれていない（開発用のダミー値のみ）
- [x] GitHub Secretsでインフラデプロイ用の認証情報を管理
- [x] Azure でのManaged Identity使用を推奨

### 🔄 修正済み（今回の修正）

- [x] Google OAuth Client IDを環境変数化
- [x] 本番環境のストレージURLを環境変数化  
- [x] .gitignoreに環境変数ファイル（.env*）を追加
- [x] .gitignoreに設定ファイル（appsettings*.json）を追加
- [x] .gitignoreにログファイルを追加

### 📋 推奨される追加対応

- [ ] Azure Key Vault統合の実装
- [ ] Static Web Appsでの環境変数設定
- [ ] セキュリティスキャンツールの導入
- [ ] 定期的な機密情報のローテーション

## 環境別設定

### ローカル開発環境

```bash
# .env.local ファイル（.gitignoreに含まれるため安全）
GOOGLE_OAUTH_CLIENT_ID=233960289934-6b9n1qacd622qnsearludssoturlqiq3.apps.googleusercontent.com
BLOB_BASE_URL=http://localhost:10000/devstoreaccount1/$web
```

### Azure Static Web Apps

Azure Portal > Static Web Apps > 構成で以下を設定：

```
GOOGLE_OAUTH_CLIENT_ID=<実際のプロダクション用Client ID>
BLOB_BASE_URL=<実際の本番ストレージURL>
```

### Azure Functions

Azure Portal > Function App > 構成で設定するか、ARM/Bicepテンプレートで管理。

## セキュリティインシデント対応

### 機密情報が誤ってコミットされた場合

1. **即座にシークレットを無効化**
2. **Git履歴から完全に削除**
3. **新しいシークレットを生成して更新**
4. **影響範囲の調査と報告**

### 参考リンク

- [GitHub Secrets Management](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [Angular Environment Variables](https://angular.io/guide/build#configure-environment-specific-defaults)

---

**最終更新日：** 2025-12-31