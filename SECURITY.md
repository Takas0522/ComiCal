# Security Policy

ComiCal を安心してご利用いただけるよう、脆弱性報告に対しては責任ある開示プロセスを採用します。

## サポート対象バージョン

`main` ブランチおよび直近のリリース（`v*`）に対してセキュリティ修正を提供します。

| Version | Supported |
|---|---|
| `main`  | :white_check_mark: |
| 最新リリース | :white_check_mark: |
| それ以前 | :x: |

## 脆弱性の報告

**公開 Issue では報告しないでください。**

GitHub の **Private Vulnerability Reporting** を利用してご報告ください:

1. このリポジトリの **Security** タブを開く
2. **"Report a vulnerability"** から非公開で詳細を送信

報告には以下を含めてください:
- 影響を受ける箇所（ファイル / エンドポイント / バージョン）
- 再現手順 / PoC（あれば）
- 想定される影響範囲
- 提案する修正方針（あれば）

## 対応プロセス

1. **3 営業日以内**に受領を確認します。
2. 重大度を評価し、修正方針を策定します。
3. **90 日以内**を目標に修正と開示を行います（責任ある開示）。
4. 修正リリース後、報告者に謝意を表明します（希望される場合）。

## 範囲外

- 楽天 Books API そのものの脆弱性 → 楽天株式会社へご報告ください。
- Azure 基盤サービスの脆弱性 → Microsoft Security Response Center (MSRC) へご報告ください。

## ハードニング方針

- TLS 1.2+ 強制 / HSTS / CSP / 出力エンコーディング
- シークレットは Key Vault + Managed Identity 経由のみ
- Dependabot / CodeQL を CI で運用
- GitHub Actions は SHA でピン留め（サプライチェーン攻撃対策）
- SBOM (CycloneDX) をリリース成果物に添付
