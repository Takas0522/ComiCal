# i18n strategy

ComiCal は **`@angular/localize` を採用** し、テンプレートの `i18n` 属性 / `$localize` テンプレートリテラルからメッセージを抽出します（`ngx-translate` / `transloco` は採用しない）。

## 現状（フェーズ 3）

- 単一ロケール **`ja-JP`** のみ。`sourceLocale = ja-JP` を `angular.json` に明示。
- ビルドは単一バンドル（`ng build`）。`--localize` フラグや i18n 用 configurations は付与していない。
- グローバル `LOCALE_ID` は Angular のデフォルト解決に任せるが、明示的に設定したい場合は `provideLocaleId()`（`./locale-id.token.ts`）を `app.config.ts` の `providers` に追加してください。

```ts
import { provideLocaleId } from './core/i18n/locale-id.token';

export const appConfig: ApplicationConfig = {
  providers: [
    provideLocaleId(), // 'ja-JP'
    // ...
  ],
};
```

## マルチロケール化の手順（2 番目のロケール追加時）

1. `src/locale/messages.<locale>.xlf` を作成し、翻訳を投入。
2. `angular.json` の `projects.frontend.i18n.locales` にエントリ追加。
3. `architect.build.configurations` にロケール別エントリを追加し、`localize: ["<locale>"]` を指定。
4. CI / SWA デプロイをロケール別バンドル（`dist/frontend/<locale>/`）配信に切り替え（パスプレフィックスまたは Accept-Language ルーティング）。
5. SSR 側で `LOCALE_ID` をリクエストごとに動的解決する場合は、`server.ts` の Express ハンドラで `provideLocaleId(detectedLocale)` を AppConfig にマージする。

## ID 命名規約

`@@<area>.<element>.<purpose>` 形式（kebab-friendly）。例:

- `@@common.button.loading`
- `@@nav.primary.label`
- `@@home.cta.subscriptions`

ID を変更すると翻訳が失われるため、リネームは慎重に。
