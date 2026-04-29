# Locale files (XLIFF 1.2)

ComiCal は当面 **日本語のみ（`ja-JP`）** の単一バンドル運用ですが、将来のロケール追加に備え `@angular/localize` の抽出構造を整備しています。

## ファイル

| ファイル | 役割 |
|---|---|
| `messages.xlf` | テンプレート抽出元（`source-language=ja-JP`）。`pnpm run extract-i18n` で再生成。手動編集禁止。 |
| `messages.ja.xlf` | `ja-JP` の翻訳ファイル。現状は `<target>` を `<source>` と同一にしているプレースホルダ。 |

## 抽出コマンド

```bash
pnpm --filter frontend run extract-i18n
# または
pnpm --filter frontend exec ng extract-i18n --output-path=src/locale
```

`i18n` 属性 / `i18n-*` 属性 / `$localize` テンプレートリテラルから自動抽出されます。`@@key` 形式で安定 ID を必ず付けてください（例: `@@home.cta.subscriptions`）。

## 新しいロケールの追加手順

1. `src/locale/messages.<locale>.xlf` を `messages.xlf` からコピーして作成。
2. `<target>` 要素を翻訳済み文字列で埋める（`state="translated"` を付与推奨）。
3. `angular.json` の `projects.frontend.i18n.locales` にエントリを追加。
4. `angular.json` の `architect.build.configurations` に `<locale>` 用設定を追加し、複数ロケールビルド（`ng build --localize`）に切り替え。
5. SSR の `LOCALE_ID` 切替戦略は `src/app/core/i18n/README.md` を参照。

## ランタイム / ビルド時の差し替え

Angular は `--localize` ビルド時にロケールごとの静的バンドルを出力（`dist/frontend/<locale>/`）。SWA Hybrid の場合はリクエスト Accept-Language / パスプレフィックスでバンドルを振り分けます。詳細は `src/app/core/i18n/README.md` を参照。
