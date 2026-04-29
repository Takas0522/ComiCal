# tools/oss-report/

ComiCal の **OSS 情報ダイアログ** (`/legal/oss`) で表示する、依存 OSS の一覧 (`oss-report.json`) を生成するスクリプトを格納します。

## 役割

`tools/sbom/` で生成された CycloneDX SBOM (`sbom-*.cdx.json`) を入力に、ダイアログ表示に最適化したフラットな JSON を出力します。

## 出力スキーマ

```json
[
  {
    "name": "Microsoft.Extensions.Logging",
    "version": "10.0.0",
    "license": "MIT",
    "url": "https://github.com/dotnet/runtime"
  }
]
```

| フィールド | 説明 |
|-----------|------|
| `name` | パッケージ名 |
| `version` | バージョン |
| `license` | SPDX ライセンス ID / expression。解決不能の場合は `"UNKNOWN"` |
| `url` | ホームページ または VCS URL。なければ空文字列 |

`name` + `version` + `license` の組で重複排除し、`name` の昇順でソート済み。

## 使い方

依存: `bash` / `jq`。

```bash
# 1) SBOM を生成（または CI artifact をダウンロード）
mkdir -p sbom
dotnet CycloneDX src/backend/ComiCal.sln -o sbom -f sbom-backend.cdx.json --json
npx --yes @cyclonedx/cyclonedx-npm \
  --output-format JSON \
  --output-file sbom/sbom-frontend.cdx.json src/frontend

# 2) OSS レポートを生成
bash tools/oss-report/generate-oss-report.sh sbom oss-report.json
```

引数:

```
generate-oss-report.sh <sbom-input-dir> <output-file>
```

入力ディレクトリ直下の `sbom-*.cdx.json` を全て読み込みます。

## CI 連携

`.github/workflows/sbom.yml` が SBOM 生成後に本スクリプトを呼び出し、`oss-report.json` を artifact / リリース成果物に同梱します。

## Angular 側の利用

将来 (Phase 3 OSS ダイアログ) に Angular のビルド時アセットとして取り込み、`/legal/oss` ページがレンダリングします。月次で再生成し、PR で更新する運用を想定。
