# ComiCal Frontend (Angular)

Angular 21ベースのフロントエンドアプリケーション。漫画検索・表示機能を提供します。

## 開発環境での起動

### 通常の開発サーバー

```bash
npm install
npm run start
```

- URL: http://localhost:4200/
- APIプロキシ: `proxy.conf.json` で http://localhost:7071 に自動転送
- 変更時の自動リロードが有効

### Azure Static Web Apps エミュレーター

```bash
npm run start:swa
```

- Azure Static Web Apps のローカルエミュレーターで起動
- API統合のテストに使用

## 環境設定

### 環境ファイル

- `src/environments/environment.ts` - 開発環境設定
- `src/environments/environment.prod.ts` - 本番環境設定

主要な設定項目：
- `apiBaseUrl`: Azure Functions APIのベースURL
- `blobBaseUrl`: Blob Storageの画像ベースURL

### DevContainer での画像URL設定

DevContainerでは、画像表示用に以下のURL設定を使用：
- **コンテナ内**: `http://azurite:10000/devstoreaccount1/images`
- **ホストアクセス**: `http://localhost:10000/devstoreaccount1/images`

## 主要コンポーネント

- **検索コンポーネント**: キーワード・発売日による漫画検索
- **一覧コンポーネント**: 検索結果の表示（画像・タイトル・著者・発売日）
- **フィルターコンポーネント**: 検索条件の設定

## ビルド

```bash
npm run build
```

成果物は `dist/` ディレクトリに出力されます。

## テスト

```bash
npm run test
```

Karma + Jasmineでのユニットテストを実行します。

## Linting

```bash
npm run lint
```

ESLintによるコード品質チェックを実行します。

## 技術スタック

- **Angular**: 21.x
- **TypeScript**: 5.x系
- **Angular Material**: UIコンポーネント
- **Angular CDK**: 共通開発キット
