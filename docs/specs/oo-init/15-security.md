# 15. セキュリティ仕様

## 15.1 OWASP Top 10 対策

| 項目 | 対策 |
|---|---|
| A01 Broken Access Control | SWA Auth + Functions ミドルウェアでロール検証。`UserId` を URL に出さず、サーバ側で principal から解決 |
| A02 Cryptographic Failures | TLS 1.2+ 強制、HSTS、KV 経由のシークレット管理 |
| A03 Injection | **EF Core パラメタライズドクエリ**、生 SQL は禁止（やむを得ない場合は `FromSqlInterpolated` のみ）|
| A04 Insecure Design | Threat Modeling を ADR で記録、Feature Flag で段階リリース |
| A05 Security Misconfiguration | Bicep で構成宣言、`what-if` でドリフト検出 |
| A06 Vulnerable Components | **Dependabot + CodeQL** を CI で必須化 |
| A07 Identification & Auth Failures | Entra External ID、Anti-CSRF（Same-Site Cookie）、セッション再生成 |
| A08 Software & Data Integrity | GitHub Actions を **SHA でピン留め**、SBOM 自動生成 |
| A09 Logging & Monitoring Failures | Application Insights + Workbook + Alert |
| A10 SSRF | 楽天 API 以外の外部 URL を直接 fetch しない、表紙 URL はホワイトリスト検証 |

## 15.2 HTTP セキュリティヘッダ

`staticwebapp.config.json` で以下を宣言:

| ヘッダ | 値 |
|---|---|
| `Content-Security-Policy` | `default-src 'self'; img-src 'self' https://*.blob.core.windows.net data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self' https://*.applicationinsights.azure.com; frame-ancestors 'none'` |
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `camera=(), geolocation=(), microphone=()` |
| `X-Frame-Options` | `DENY`（CSP `frame-ancestors` のフォールバック）|

## 15.3 シークレット管理

- **コードへのハードコード禁止**。
- すべてのシークレットは **Key Vault** に格納し、App Settings に **Key Vault 参照** で注入。
- Functions / SWA は System Assigned Managed Identity で KV からシークレット取得。
- ローカル開発: `local.settings.json` は **ignore**、`.sample` のみコミット。

## 15.4 出力エンコーディング / XSS

- Angular の標準サニタイザ + Tailwind v4 でインライン JS 排除。
- `[innerHTML]` の使用には `DomSanitizer` を必須化。lint ルールでブロック。

## 15.5 CSRF

- Cookie は `SameSite=Lax` 既定。
- 状態変更 API (POST / PUT / DELETE) は **SSR を経由してのみ呼び出す**（Functions は SWA Linked + `Authorization=function`）。

## 15.6 データ保持 / 削除

| データ | 保持 |
|---|---|
| 匿名利用データ | クラウド未保存、端末ローカル（IndexedDB）のみ |
| ログインユーザー（Subscriptions / Purchases）| 退会まで永久保存 |
| ソフト削除されたユーザー | **30 日後にハード削除**（バッチ）|
| 楽天 API レスポンスの生ログ | App Insights 既定保持期間のみ（30 日 / 90 日）|
| Application Insights ログ | dev 30 日 / prod 90 日 |

## 15.7 PII 最小化

- 監査ログには UserId（GUID）のみ。IdP Subject はログ出力時にハッシュ化。
- メールアドレス / 表示名はログに出さない。
- 個人特定可能なクエリは禁止（特に検索文字列の生ログ化禁止）。

## 15.8 サプライチェーン

- GitHub Actions の **action は SHA でピン留め**（タグ参照は禁止）。
- npm / NuGet 依存は Dependabot で自動 PR。
- **SBOM** を CI で生成（CycloneDX）。`tools/sbom/` で構成、リリース成果物に同梱。
- ライセンス情報を `tools/oss-report/` で集約し、`/legal/oss` ページに反映。

## 15.9 脅威モデル更新

- 新規外部統合 / 認可境界の変更は ADR + 軽量 STRIDE を必須。
- 大きな変更時は ADR を `docs/adr/` に追加（`write-adr` Skill）。

## 15.10 インシデント対応

- `SECURITY.md` に脆弱性報告窓口を明記。
- 90 日以内に修正 / 開示の責任ある開示ポリシーを採用。
