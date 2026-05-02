# 05. ディスカバリ / 共有

## 5.1 MVP 方針

- **MVP では実装しない**。すべて feature flag (Azure App Configuration) 下で OFF 起動。
- フロント・バックエンドのコードベースに「ディスカバリ / 共有」のシード実装は持たず、将来の拡張ポイント（インターフェース）のみを残す。

## 5.2 将来検討項目（フラグ予約名）

| Feature Flag 名 | 内容 | 想定 UI |
|---|---|---|
| `discovery.recommend` | 購読履歴に基づく類似シリーズのレコメンド | ホーム下部「あなたへのおすすめ」セクション |
| `discovery.trending` | 全ユーザーの購読数ランキング（プライバシー配慮で集計値のみ）| ホーム下部「今、注目されている」 |
| `sharing.og-card` | シリーズ詳細の OG 画像生成（公開リンク）| シリーズ詳細にシェアボタン |
| `sharing.public-link` | ユーザーの公開購読リスト | 設定で公開可否を選択 |

## 5.3 フラグ運用ルール

- 各フラグは **MVP リリース時点で OFF 固定**。
- 削除予定日 (`removalDate`) を `infra/modules/app.bicep` の `appConfig.featureFlags` に記録。
- ON 切替は ADR を起こしたうえでのみ実施。

## 5.4 関連設計ガイド

- 個別の機能を追加する際は `.claude/skills/add-feature-flag` Skill を使い、`IFeatureManager`（.NET）と Angular signal-based のラッピングを同時更新する。
