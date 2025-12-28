# Azurite Image Seeding

このディレクトリには、DevContainer環境のAzurite Blob Storageに画像データをシードするためのスクリプトが含まれています。

## ファイル

- **seed-images.js** - プレースホルダー画像をアップロードするスクリプト（DevContainer起動時に自動実行）
- **seed-images-from-api.js** - 楽天APIから実際の画像をダウンロードしてアップロードするスクリプト
- **package.json** - Node.js依存関係定義

## 使用方法

### 自動実行（推奨）

DevContainerを起動すると、`seed-images.js`が自動的に実行され、プレースホルダー画像がAzuriteにアップロードされます。

### 手動実行

#### プレースホルダー画像のアップロード

```bash
cd /workspaces/ComiCal/database
npm install
npm run seed-images
```

#### 楽天APIから実際の画像をダウンロード

```bash
cd /workspaces/ComiCal/database
npm install
RAKUTEN_APP_ID=your_app_id npm run seed-images-from-api
```

## 仕組み

### seed-images.js

1. Azurite Blob Storageに接続
2. `images`コンテナを作成（存在しない場合）
3. seed.sqlの20件のISBNに対してループ
4. 各ISBNについて、画像が既に存在するかチェック
5. 存在しない場合、1x1透明PNGをプレースホルダーとしてアップロード

### seed-images-from-api.js

1. Azurite Blob Storageに接続
2. `images`コンテナを作成（存在しない場合）
3. seed.sqlの20件のISBNに対してループ
4. 各ISBNについて：
   - 楽天ブックスAPIで書籍情報を取得
   - 画像URLを取得（largeImageUrl優先）
   - 画像をダウンロード
   - Azuriteにアップロード
5. APIレート制限を考慮して200ms待機

## 環境変数

- `STORAGE_CONNECTION_STRING` - Azure Storage接続文字列（デフォルト: Azurite）
- `RAKUTEN_APP_ID` - 楽天APIアプリケーションID（seed-images-from-api.jsのみ必須）

## トラブルシューティング

### "Cannot connect to Azurite"

Azuriteが起動していることを確認してください：

```bash
curl http://azurite:10000/
docker logs comical-azurite
```

### "RAKUTEN_APP_ID is not set"

楽天APIを使用する場合は、環境変数を設定してください：

```bash
export RAKUTEN_APP_ID=your_app_id
npm run seed-images-from-api
```

### 画像を確認したい

Azuriteのデータは`.devcontainer/data/azurite/`に保存されています。

## 本番環境への展開

本番環境では、Azure Blob Storageを使用してください：

1. `STORAGE_CONNECTION_STRING`に本番のAzure Storage接続文字列を設定
2. seed-images-from-api.jsを実行して実際の画像をアップロード
3. または、既存のAzure Storageから画像をコピー

**注意:** 本番環境では`seed-images.js`（プレースホルダー）を使用しないでください。
