---
name: add-feature-flag
description: 'Use when adding an Azure App Configuration feature flag for staged rollout or kill switch. Wires Feature Manager into .NET 10 Functions (IFeatureManager) and Angular v21 (signal-based), registers the flag in infra/modules/app.bicep, and enforces kebab-case naming, removal date tracking, and ON/OFF test coverage.'
argument-hint: '<flagName-kebab>'
allowed-tools: Read, Write, Edit, Bash
---

# add-feature-flag

## 命名規約

- kebab-case: `subscription-csv-export`、`enable-rakuten-v2-client`
- 機能名 + 動詞/形容詞。プロダクトドメインで意味が伝わるように

## バックエンド (.NET 10 Functions)

1. **App Configuration の Feature Manager** を有効化
2. `Program.cs` で `AddAzureAppConfiguration().UseFeatureFlags()` と `AddFeatureManagement()`
3. 利用箇所:
   ```csharp
   if (await _featureManager.IsEnabledAsync("subscription-csv-export"))
   {
       // new behavior
   }
   ```
4. テスト: `IFeatureManager` をモック

## フロント (Angular v21)

1. アプリ起動時に `/api/feature-flags` から取得（または App Configuration の SWA SDK）
2. Signal で保持: `readonly featureFlags = signal<Record<string, boolean>>({});`
3. テンプレート:
   ```html
   @if (featureFlags()['subscription-csv-export']) {
     <button data-testid="export-csv">CSV エクスポート</button>
   }
   ```

## App Configuration / Bicep

- フラグキーを `infra/modules/app.bicep` 内の App Configuration に登録
- 環境ごとに dev: ON、prod: OFF など `bicepparam` で制御

## ライフサイクル

- **作成時**: 削除予定日（≤ 90 日）を CHANGELOG / ADR に明記
- **削除時**: コードから参照を完全除去 → App Configuration からも削除

## チェックリスト

- [ ] 命名が kebab-case
- [ ] バックエンド `IFeatureManager` 経由で参照
- [ ] フロント Signal 経由で参照
- [ ] App Configuration に登録（Bicep）
- [ ] 削除予定日と除去手順を ADR or Issue に記録
- [ ] 単体テスト/E2E で ON/OFF 両系統をカバー

## アンチパターン

- ❌ 環境変数で直接フラグを切り替える
- ❌ コード内に `// TODO: remove flag` のみ書いて消し忘れる
- ❌ フラグ名が技術的 (`use-new-impl`) で意図不明
- ❌ ON/OFF 片方しかテストしない
