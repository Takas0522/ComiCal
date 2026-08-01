# 0001. 直近の発売予定一覧に対するキーワード絞り込みと復元

- **Status**: Proposed
- **Date**: 2026-07-26
- **Deciders**: @Takas0522
- **Related Issue**: [#302](https://github.com/Takas0522/ComiCal/issues/302)

## Context and Problem Statement

「直近の発売予定」画面（`/` = `HomePage`。`/api/v1/volumes/upcoming` を利用）と
カレンダー画面（`/calendar`。`/api/v1/volumes/calendar` を利用）では、タイトル・著者で
発売予定を継続的に絞り込めず、目的の巻へ到達するまでのコストが高い。

既存の OpenAPI と機能仕様は `/series?q=` を「タイトルまたは著者」と説明しているが、
現行 `SeriesRepository.SearchAsync` は `Series.NormalizedTitleHiragana` のみを検索し、
Authors / SeriesAuthors を結合していない。本 ADR の対象は発売予定 API であり、この
既存の仕様・実装不整合を同時に変更しない。ただし、Issue #302 の要件に従い、新設する
`/volumes/upcoming?q=` と `/volumes/calendar?q=` はタイトル**または著者**を検索する。

Issue #302 は次を要求している:

1. 発売予定一覧画面上でタイトル/著者キーワードによる絞り込みを行う
2. 入力キーワードを保存し、次回訪問/リロード時に自動復元する
3. クリアすれば保存値も破棄する
4. MVP はクライアント保存（ログイン非依存）とし、将来サーバー保存へ拡張可能

本 ADR は、UI・API・ストレージ・検索実装の各レイヤの設計方針を確定する。

## Decision Drivers

- **既存アーキテクチャ整合**: Clean Architecture + Repository、Angular v21 Standalone + Signals、
  Database First (SSDT)、SQL FullText 検索という既存パターンを崩さない
- **文字正規化の整合**: 既存検索と同じ `fnToHiragana` + `CONTAINS` パターンを使い、
  ひらがな/カタカナの揺れを吸収する
- **保存レイヤの拡張性**: 両画面で共有するキーワード配列を IndexedDB に保存し、後日
  「ユーザー設定」としてサーバー保存へ移行可能なインターフェースを提供
- **後方互換**: 既存の `/volumes/upcoming` / `/series?q=` の呼び出し元を壊さない
- **パフォーマンス**: `LIKE '%…%'` を避け、FullText Index を使う（DB 規約準拠）
- **SSR / Zoneless / OnPush** への適合（IndexedDB への非同期アクセスと SSR フォールバック）
- **アクセシビリティ (WCAG 2.1 AA)**: チップ入力のラベル・キーボード操作・個別削除・
  結果件数のライブ通知

## Considered Options

### API — キーワード絞り込みの実装場所

- **API-A（採用）**: `/volumes/upcoming` と `/volumes/calendar` に JSON 配列の `q` クエリを
  追加し、サーバー側で Series (タイトル) / Author (著者名) を FullText で絞り込む
- API-B: フロント側で `/volumes/upcoming` 取得後に JS フィルタする
- API-C: 新エンドポイント `/volumes/upcoming/search` を新設

### 検索実装

- **Search-A（採用）**: `q` 配列の各語を `dbo.fnToHiragana` で正規化して
  `CONTAINS` のフレーズ先頭一致にし、タイトルまたは全著者ロールに一致する Series を
  OR で結合して Volume を絞る
- Search-B: `Series.Title LIKE '%q%' OR Author.Name LIKE '%q%'`（規約違反・低速）
- Search-C: Azure AI Search を導入（オーバースペック・コスト増）

### クライアント保存

- **Storage-A（採用）**: `idb-keyval` を使い IndexedDB に `upcoming-filter-keywords` キーで
  `string[]` を保存。抽象化のため `UpcomingFilterStore` サービス（Signals ベース）を導入し、
  実装差し替えでサーバー保存に移行可能とする
- Storage-B: `sessionStorage`（タブ閉鎖で消失、要件4を満たさない）
- Storage-C: IndexedDB API を直接利用（`idb-keyval` より実装量が増える）
- Storage-D: 現時点でサーバー保存（`/me/preferences` 新設）— MVP スコープ外

### キーワード管理 UI

- **UI-A（採用）**: 新規 `/settings/keywords` 画面をキーワードの追加・編集・削除の唯一の
  管理面とし、HomePage / CalendarPage は保存済み条件を適用する閲覧面とする
- UI-B: HomePage / CalendarPage 上で直接編集する（閲覧と管理が混在する）
- UI-C: 検索画面からしか編集できない（既存キーワードの一覧管理ができない）

## 確定事項

1. 対象画面は HomePage と CalendarPage の両方とする。
2. 両画面は同一の保存済みキーワード配列を共有する。
3. CalendarPage の日付確定巻は選択中の週/月レンジ内だけを検索する。
4. 各キーワードは前後空白を除去し、空白だけの要素は削除する。
5. 要素内に空白がある語は、語順どおりのフレーズとして検索する。
6. 一致方法は正規化後の先頭一致とする。
7. FullText 構文に影響する文字は検索演算子として扱わず、安全に無効化する。
8. API は複数キーワードを受け付ける。
9. 複数キーワードの一致は OR 条件とする。
10. API の `q` は URL エンコードした JSON 文字列配列とする。
11. JSON 配列内の空要素は除外する。
12. Unicode FormKC（ブラウザーでは NFKC）正規化・前後トリム後の重複を除外し、最大 16 語かつ全要素合計を 512 文字以下に制限する。
13. 不正な JSON、配列以外、文字列以外の要素は `400 Problem+JSON` を返す。
14. 空要素除外後に空配列となった場合は、フィルタなしとして扱う。
15. 専用管理画面の入力 UI は追加・個別編集・削除ができるチップ形式とする。
16. IndexedDB への保存は、検索画面での明示的な登録または専用管理画面での変更時に行う。
17. CalendarPage の発売日未定巻もキーワードで絞り込む。
18. キーワード指定時の一致する発売日未定巻は、選択中の週/月に関係なく全件表示する。
19. 著者検索は Primary / Co / Original の全ロールを対象にする。
20. キーワード指定かつ「購読中のみ」で 0 件の場合、購読シリーズ未登録の案内を優先する。

## Decision Outcome

採用案: **API-A + Search-A + Storage-A + UI-A**

### Rationale

- 既存 `SeriesRepository.SearchAsync` のひらがな変換 + FullText パターンと
  `FT_Series` / `FT_Authors` の資産を再利用でき、DB 規約（LIKE 禁止・FullText 使用）に
  自然に整合する
- `idb-keyval` はすでにフロントエンド依存として導入済みであり、匿名利用の保存に
  IndexedDB を使うプロジェクト規約に従える
- `UpcomingFilterStore` を介することで、将来サーバー保存へ差し替える際に UI 側の
  変更を最小化できる
- 検索時の文字正規化を既存パターンに揃えることで、ひらがな/カタカナの揺れに対する
  QA コストを削減

### Consequences

- ✅ Positive
  - タイトル/著者どちらでも既存 FT インデックスで高速絞り込みが可能
  - ログイン非依存で即時利用可能
  - 既存 `/search` のリクエスト・応答契約を変更しない
  - 将来のサーバー保存拡張時、フロントは Store 実装差し替えのみで済む
- ⚠️ Negative / Trade-off
  - IndexedDB のためデバイス/ブラウザ間で共有されず、初期復元は非同期になる
    （将来 Storage-D で解消）
  - サーバー側で Upcoming / Calendar の Query にキーワード配列を追加するため、
    Repository・UseCase・Function・OpenAPI の複数レイヤに変更が入る
  - FullText 条件は最大 16 語（各語はタイトルまたは著者）で OR 結合するため、上限内でも検索 SQL は一定の複雑さを持つ

## 詳細設計

### 1. API 仕様

`GET /api/v1/volumes/upcoming` と `GET /api/v1/volumes/calendar` に `q` を追加する。
`q` は URL エンコードした JSON 文字列配列であり、例は
`?q=%5B%22%E4%BD%9C%E5%93%81%E5%90%8D%22%2C%22%E8%91%97%E8%80%85%E5%90%8D%22%5D`
（復号後: `["作品名","著者名"]`）とする。

| Query         | Type                         | Required | Description                                               |
| ------------- | ---------------------------- | -------- | --------------------------------------------------------- |
| `cursor`      | string                       | no       | upcoming の既存 `(ReleaseDate, VolumeId)` keyset カーソル |
| `limit`       | int                          | no       | upcoming の既存。1–100、default 30                        |
| `from` / `to` | string (ISO 8601 date)       | no       | calendar の既存日付レンジ                                 |
| **`q`**       | **URL エンコード JSON 配列** | **no**   | **タイトル/著者キーワード配列**                           |

- `q` の JSON は文字列配列だけを受け付ける。不正 JSON、配列以外、文字列以外の要素は
  RFC 7807 の `400 Problem+JSON` を返す
- 各要素は前後空白を除去し、空要素を除外する。残りが空配列ならフィルタなしとして扱う
- Unicode FormKC（ブラウザーでは NFKC）正規化・前後トリム後の重複を除外した配列は最大 16 語、全要素の合計文字数は 512 文字以下とする。超過時は `400 Problem+JSON` を返す
- 配列内の二重引用符など FullText 構文に影響する文字は空白に置換し、検索式として解釈しない
- 各要素はフレーズ先頭一致、配列要素間は OR 条件とする。各要素はタイトルまたは
  Primary / Co / Original のいずれかの著者ロールに一致すればよい
- `q` 指定時のみ絞り込みを有効化。未指定または空配列時は現行仕様と完全に同一（後方互換）
- 一致 0 件は `200 OK` + `{ items: [], nextCursor: null }`
- upcoming のレスポンス schema は既存 `VolumePagedResult` を再利用する
- 「購読中のみ」は既存どおり HomePage が取得済みの結果に対してクライアント側で
  絞り込む。現行 `/volumes/upcoming` に `subscribedOnly` パラメータは存在しない
- calendar は `q` 指定時、選択中の週/月レンジ内の発売日確定巻に加え、一致する
  発売日未定巻を全件 `undatedVolumes` に返す。`q` 未指定時の既存レスポンスは変更しない

OpenAPI (`docs/api/openapi.yaml`) は `getUpcomingVolumes` と `getCalendarVolumes` の
parameters に `q` を追加する。また、実装と乖離している calendar の `year` / `month` /
`week` 契約を、実際に使用する `from` / `to` 契約へ是正する。

### 2. バックエンド変更点

**Domain** — `UpcomingQuery` と `CalendarQuery` に `Keywords` を追加:

```csharp
public sealed record UpcomingQuery(
    string? Cursor,
    int PageSize = 20,
    IReadOnlyList<Guid>? FilterBySeriesIds = null,
    IReadOnlyList<string>? Keywords = null);

public sealed record CalendarQuery(
    int Year,
    int Month,
    int? Week = null,
    IReadOnlyList<Guid>? FilterBySeriesIds = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    IReadOnlyList<string>? Keywords = null);
```

**API** — `KeywordQueryParser` を新設し、両 Function から呼び出す。JSON の解析・要素型・
Unicode 正規化済みかつトリム済みの重複除外・最大 16 語・合計 512 文字を検証し、失敗時は
RFC 7807 の `400 Problem+JSON` を返す。
既存の query validator は DI 登録されるだけで Function / UseCase から自動実行されないため、
この境界検証を省略しない。

**Infrastructure** — `VolumeRepository.GetUpcomingAsync` を拡張:

```csharp
if (query.Keywords?.Count > 0)
{
    // JSON 配列を 1 パラメーターとして OPENJSON に渡し、dbo.fnToHiragana で
    // 一括正規化してからフレーズ先頭一致の FullText 条件を構築する。複数条件は OR 結合する。
    var terms = await NormalizeFullTextTermsAsync(query.Keywords, ct);
    q = q.Where(v => MatchesAnyKeyword(v.SeriesId, terms));
}
```

- ソート順・keyset は現行 `(ReleaseDate, VolumeId)` を維持
- `NormalizeFullTextTermsAsync` は JSON 配列の `OPENJSON` により全語を 1 回の
  パラメータ化済み DB 操作で正規化する。キーワードごとの DB 往復は行わない
- `MatchesAnyKeyword` は、各 term についてタイトル一致または `SeriesAuthors` を経由した
  全著者ロール一致を判定し、Series / Author の論理削除を除外する。全 term は OR 結合する
- `Author` テーブルには `IsDeleted` 列があるが、現在の Domain Entity / EF Configuration
  はこの列をマップしていない。この実装に先立ち `Author.IsDeleted` / `DeletedAt` と
  EF マッピングを追加し、論理削除済みの著者を必ず検索結果から除外する
- CancellationToken は既存どおり伝搬

**Calendar Repository** — `q` 指定時は `ReleaseDate` がレンジ内の巻と `ReleaseDate IS NULL`
の巻を取得してから同じキーワード条件を適用し、前者を `days`、後者を `undatedVolumes` に
分ける。`q` 未指定時は既存のレンジ条件を維持する。

**API 層** — `VolumesFunctions.GetUpcomingAsync` と `GetCalendarAsync`:

```csharp
var keywords = KeywordQueryParser.Parse(req.GetQueryParam("q"));
var upcomingQuery = new UpcomingQuery(cursor, limit > 0 ? limit : 30, null, keywords);
var calendarQuery = new CalendarQuery(year, month, week, null, fromDate, toDate, keywords);
```

### 3. フロントエンド設計

**新規サービス** — `src/frontend/src/app/features/upcoming-filter.store.ts`:

```ts
@Injectable({ providedIn: "root" })
export class UpcomingFilterStore {
  private static readonly KEY = "upcoming-filter-keywords";
  private readonly platformId = inject(PLATFORM_ID);
  readonly keywords = signal<readonly string[]>([]);
  readonly restored = signal(false);

  async restore() {
    if (!isPlatformBrowser(this.platformId)) {
      this.restored.set(true);
      return;
    }
    this.keywords.set((await get<string[]>(UpcomingFilterStore.KEY)) ?? []);
    this.restored.set(true);
  }

  async addKeyword(keyword: string) {
    /* trim, deduplicate, then persist */
  }
  async updateKeyword(index: number, keyword: string) {
    /* trim, deduplicate, then persist */
  }
  async removeKeyword(index: number) {
    /* remove, then persist */
  }
  async clearKeywords() {
    this.keywords.set([]);
    if (!isPlatformBrowser(this.platformId)) return;
    await del(UpcomingFilterStore.KEY);
  }
}
```

- `get` / `set` / `del` は `idb-keyval` から import する。SSR では `isPlatformBrowser()` で
  IndexedDB アクセスを防ぎ、`restore()` は空配列で完了する
- HomePage は `ngOnInit` で `await store.restore()` してから最初の API 要求を行う。
  CalendarPage は現行コンストラクタの `effect()` を、復元完了 Signal を条件に含めた effect に
  変更し、復元前に未絞り込みの要求を発行してから絞り込み結果で上書きする二重要求を避ける
- `addKeyword` / `updateKeyword` は、空要素を拒否し、既存値と重複する語を追加・更新しない。
  保存前に最大 16 語かつ合計 512 文字を検証し、超過時は UI に通知できる失敗結果を返す
- Signal 化により SearchPage / KeywordsSettingsPage / HomePage / CalendarPage から同じ状態を参照できる
- 将来サーバー保存化時は本クラスの内部実装のみ差し替え

**新規 Molecule** — `src/frontend/src/app/molecules/keyword-filter/keyword-filter.component.ts`:

- チップ配列を `input<readonly string[]>()` として受け取り、入力欄で Enter を押すと
  前後空白を除いた語をチップに追加する。重複語は追加しない
- 各チップにキーボード操作可能な編集・削除ボタンを置く。編集時は input を表示し、
  Enter で確定、Escape で取り消す
- 16 語または合計 512 文字を超える操作は保存せず、入力欄にエラーメッセージを表示する。
  API 側の 16 語・512 文字検証は必ず維持する
- テスト ID は `keyword-filter-input`、`keyword-filter-chip-edit`、
  `keyword-filter-chip-remove` とする
- `aria-label`、チップ数の `aria-live="polite"` 通知、編集・削除ボタンの語を含むラベルを提供する

**新規 Page** — `src/frontend/src/app/pages/keywords-settings/keywords-settings.page.ts`:

- route は `/settings/keywords`、title は「絞り込みキーワード | まんがリマインダー」とする
- `KeywordFilterComponent` を配置し、キーワードの追加・編集・削除を `UpcomingFilterStore` に委譲する
- 空状態では「キーワードを登録すると、ホームとカレンダーの発売予定を自動で絞り込めます。」を表示する
- `SettingsPage` に「絞り込みキーワード」への導線を追加する

**`SearchPage` の変更**:

- 既存 `SearchBarComponent` によるフリーワード検索は維持する
- 検索語が空でなく、保存済みキーワードと重複しない場合にのみ
  「『{検索語}』を絞り込みキーワードに登録」ボタンを表示する
- 登録ボタンは `UpcomingFilterStore.addKeyword(query())` を呼び、成功時は
  「絞り込みキーワードに登録しました。」を `aria-live="polite"` で通知する
- 検索結果のシリーズ・著者をキーワードとして登録する操作は提供しない

**`HomePage` と `CalendarPage` の変更**:

- 同じ `UpcomingFilterStore.keywords` を復元して JSON 文字列にし、`q` として送信する。
  URL は `HttpParams` で構築し、文字列連結で JSON を組み込まない
- 保存済みキーワードがあるときは、適用中のチップ一覧と「キーワードを管理」リンク
  （`/settings/keywords`）を表示する。閲覧画面上では追加・編集・削除を行わない
- 0 件時は既存パターン踏襲した空状態メッセージ:
  「指定したキーワードに一致する発売予定はありません。」（絵文字 📚）
- `filteredVolumes` の `subscribedOnly` ロジックはそのまま。`q` はサーバー側で絞る
- Home の空状態 test id は `home-keyword-empty-state`、Calendar の空状態 test id は
  `calendar-keyword-empty-state` とする

**URL 同期 (任意)**: MVP では URL クエリ同期は行わない。ブックマーク経由で
特定条件を共有したい要件は Issue #302 のスコープ外。

### 4. アクセシビリティ

- `KeywordFilterComponent` の入力ラベルは「絞り込みキーワードを追加」とする
- 結果件数の変化を `aria-live="polite"` の領域で通知（既存 CardGrid の上に配置）
- キーボード: Enter で入力語をチップ追加、チップ編集は Enter で確定・Escape で取り消し、
  削除は明示的な削除ボタンで行う

### 5. テスト戦略

**Backend (xUnit + Testcontainers)** — `src/tests/backend/`:

- `GetUpcomingVolumes` の以下シナリオを追加:
  - `q` 未指定 / 空要素だけの配列 → 現行結果（回帰）
  - タイトル一致・全著者ロール一致・複数語 OR 一致 → 該当 Series のみ
  - 複数語フレーズ・先頭一致・全角/半角/カタカナの揺れ → 正規化後に期待どおり一致
  - 不正 JSON・配列以外・文字列以外の要素・合計 513 文字 → `400 Problem+JSON`
  - 一致 0 件 → 200 + 空配列
- `GetCalendarVolumes` の以下シナリオを追加:
  - 指定レンジ内の一致巻だけを `days` に返す
  - `q` 指定時だけ、一致する発売日未定巻を全件 `undatedVolumes` に返す
  - `q` 未指定時は発売日未定巻を追加せず既存結果を維持する

**Frontend (Jest)** — `src/frontend/src/app/pages/home/`:

- `UpcomingFilterStore`: IndexedDB の get/set/delete、SSR フォールバック、復元完了状態
- `KeywordFilterComponent`: Enter による追加、重複排除、個別編集・削除、16 語・512 文字制限、ARIA 属性
- `KeywordsSettingsPage`: 空状態、追加、編集、削除、設定画面からの導線
- `SearchPage`: フリーワード検索を維持し、検索語の登録・重複時の登録ボタン非表示・ライブ通知
- `HomePage` と `CalendarPage`:
  - 初期表示で保存済み JSON 配列 `q` を送信することを `HttpTestingController` で検証
  - 保存済みチップの表示と管理画面への遷移
  - 共有配列を保存後、もう一方の画面を訪問して復元・自動適用
  - 0 件時と購読シリーズ未登録時の優先空状態メッセージ

**E2E (Playwright POM)** — `src/tests/e2e/`:

- `pages/keywords-settings.page.ts` と対応 selector を追加し、キーワードの追加・編集・削除操作を
  POM に集約する
- specs:
  - 専用管理画面で複数キーワードを登録→リロードで同条件再現
  - 検索語を登録し、重複登録を防止する
  - 専用管理画面でキーワードを編集・削除する
  - Home → Calendar、Calendar → Home の各遷移で同じ保存条件を復元・自動適用
  - 全キーワード削除後リロードで未絞り込み
  - Home / Calendar の一致なし空状態と、Calendar の一致する発売日未定巻
  - `/search` 画面の既存タイトル検索・遷移の回帰スモーク
- `waitForTimeout` 禁止・`data-testid` 経由・POM 経由の規約遵守

### 6. 影響範囲サマリ

| レイヤ         | ファイル                                                                                                                                                                                                                                                        |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Domain         | `Queries/UpcomingQuery.cs`, `Queries/CalendarQuery.cs`, `Entities/Author.cs`（論理削除プロパティ追加）                                                                                                                                                          |
| Infrastructure | `Configurations/AuthorConfiguration.cs`, `Repositories/VolumeRepository.cs`                                                                                                                                                                                     |
| API            | `Functions/VolumesFunctions.cs`, `KeywordQueryParser.cs`（新規）                                                                                                                                                                                                |
| OpenAPI        | `docs/api/openapi.yaml`（upcoming / calendar の `q`、calendar の `from` / `to` 契約是正）                                                                                                                                                                       |
| Frontend       | `features/upcoming-filter.store.ts`（新規）、`molecules/keyword-filter/keyword-filter.component.ts`（新規）、`pages/keywords-settings/keywords-settings.page.ts`（新規）、`pages/{home,calendar,search}/**`、`app.routes.ts`、`pages/settings/settings.page.ts` |
| Tests (BE)     | `src/tests/backend/**`（Upcoming / Calendar のユースケースと API）                                                                                                                                                                                              |
| Tests (FE)     | `molecules/keyword-filter/keyword-filter.component.spec.ts`、`pages/{keywords-settings,home,calendar,search}/**/*.spec.ts`、`features/upcoming-filter.store.spec.ts`                                                                                            |
| Tests (E2E)    | `src/tests/e2e/pages/{keywords-settings,home,calendar,search}.page.ts`、対応 selectors / specs                                                                                                                                                                  |
| DB             | 変更なし（既存 FT インデックスを再利用）                                                                                                                                                                                                                        |
| IaC            | 変更なし                                                                                                                                                                                                                                                        |

## Validation

- 受け入れ条件（Issue #302）の API / 画面 / E2E の全チェックボックスを満たすこと
- `pnpm test` および `dotnet test` がすべて通ること（カバレッジ ≥ 80%）
- `pnpm --filter frontend generate:api` により OpenAPI から型生成が完了すること
- 手動検証: リロード・タブ再オープン・別デバイスでの挙動を確認
- パフォーマンス: 既存の API 応答時間と比較し、`q` 指定時の p95 劣化を
  Application Insights で確認する

機能フラグは導入しない。既存 App Configuration のフラグはフロントエンドで取得・評価する
仕組みが未実装であり、この MVP にフラグだけを追加してもロールアウト制御にならないためである。

## Links

- Issue: [#302 タイトルか著者名のキーワードのみで発売予定書籍を絞り込めるようにしたい](https://github.com/Takas0522/ComiCal/issues/302)
- 既存検索実装: `src/backend/infrastructure/ComiCal.Infrastructure.Sql/Repositories/SeriesRepository.cs`
- FullText Index: `src/db/FullText/FT_Series.sql`, `src/db/FullText/FT_Authors.sql`
- 既存パターン参照: `src/frontend/src/app/pages/home/home.page.ts`（`home_subscribed_only` の localStorage 利用）
- 新規検索 UI: `src/frontend/src/app/molecules/keyword-filter/keyword-filter.component.ts`
- 仕様: `docs/specs/oo-init/03-functional-requirements.md`, `docs/specs/oo-init/10-frontend-spec.md`
