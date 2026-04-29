# 19. OSS / 法務

## 19.1 ライセンス

- 本プロジェクトは **MIT ライセンス** で公開。
- ルートに `LICENSE` を配置。
- `README.md` の冒頭にバッジを表示。

## 19.2 SBOM

- CI で **CycloneDX 形式の SBOM** を自動生成（`tools/sbom/` 配下に設定）。
  - フロント: `@cyclonedx/cyclonedx-npm`。
  - バック: `CycloneDX.dotnet` (`dotnet CycloneDX`)。
- リリース成果物（GitHub Releases）に **SBOM を添付**。

## 19.3 OSS 情報ダイアログ

- `/legal/oss` 静的ページ + 軽量ダイアログ版を提供。
- 表示項目:
  - 使用 OSS パッケージ名 / バージョン / ライセンス
  - GitHub リポジトリへのリンク
- ソース: `tools/oss-report/` で生成された JSON を Angular がビルド時に読み込み、ページに描画。
- 月次で再生成し、PR で更新。

## 19.4 楽天アフィリエイト

- 楽天アフィリエイト規約に従い、以下を **必ず表示**:
  - **「Powered by Rakuten Books」** クレジット
    - **フッタに常時表示**
    - **OSS / 楽天クレジットダイアログ** に詳細表示
  - 楽天 Books へのリンクには **アフィリエイト ID 付与**。
- ユーザー設定で **アフィリエイトリンク表示 ON/OFF** を選択可能。OFF にしてもクレジット表示は維持（規約上必須）。

## 19.5 プライバシーポリシー / 利用規約

- 静的ページとして同梱:
  - `/legal/privacy` (`PrivacyPolicy.md` を Angular で描画)
  - `/legal/terms`  (`TermsOfService.md` を Angular で描画)
- 初回サインアップ時に **同意チェック** を必須化し、`Users.AgreedAt` に記録。
- 主な記載内容:
  - 取得する情報（IdP Subject、表示名、購読 / 購入データ、クッキー）。
  - 第三者提供なし。広告なし。
  - クッキー利用範囲（認証 + 設定の永続化のみ）。
  - データ保持期間（退会まで、ソフト削除後 30 日でハード削除）。
  - 楽天 Books API の利用（アフィリエイト規約に基づく）。
  - 問い合わせ窓口（GitHub Issues / Email）。

## 19.6 商標 / 著作権

- 「楽天」「Rakuten Books」の商標は楽天グループ株式会社の商標。
- 表紙画像は楽天 API から取得した素材を **アフィリエイトプロモーション目的の範囲** で再配信（規約に従う）。
- ComiCal / まんがリマインダー はプロジェクト名（商標未登録、利用は自由）。

## 19.7 コントリビューションガイド

- `CONTRIBUTING.md`（ファイル名のみ将来追加）に下記を明記:
  - Conventional Commits 必須。
  - PR は Draft で作成し、CI 通過後にレビュー依頼。
  - 重要な設計変更は **ADR** を起こす。

## 19.8 Security ポリシー

- `SECURITY.md` を同梱し、責任ある開示プロセスを記載（90 日）。
- 連絡先: GitHub Private Vulnerability Reporting / 専用メール。
