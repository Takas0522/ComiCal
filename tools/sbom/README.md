# tools/sbom/

このディレクトリは ComiCal の **Software Bill of Materials (SBOM)** 関連の設定 / 出力先を扱います。

## 概要

ComiCal は MIT ライセンスで配布される OSS であり、依存 OSS の開示義務に応えるため、**CycloneDX 形式** の SBOM を CI で自動生成しています。

## 生成タイミング

`.github/workflows/sbom.yml` が以下のトリガーで実行されます。

| トリガー | 内容 |
|---------|------|
| `push: main` | 最新 SBOM を artifact としてアップロード |
| `release: published` | GitHub Release に SBOM を添付 |
| `workflow_dispatch` | 手動実行 |

## 生成物

ワークフロー実行後、`sbom-cyclonedx` artifact に以下が含まれます。

| ファイル | 対象 | ツール |
|---------|------|------|
| `sbom-backend.cdx.json` | `src/backend/ComiCal.sln` (.NET 10) | `dotnet CycloneDX` |
| `sbom-frontend.cdx.json` | `src/frontend` (Angular v21 / pnpm) | `@cyclonedx/cyclonedx-npm` |
| `sbom-e2e.cdx.json` | `src/tests/e2e` (Playwright) | `@cyclonedx/cyclonedx-npm` |
| `oss-report.json` | OSS 情報ダイアログ用に整形した一覧 | `tools/oss-report/generate-oss-report.sh` |

## ローカル生成

```bash
# Backend
dotnet tool install --global CycloneDX
dotnet CycloneDX src/backend/ComiCal.sln -o sbom -f sbom-backend.cdx.json --json

# Frontend
npx --yes @cyclonedx/cyclonedx-npm \
  --output-format JSON \
  --output-file sbom/sbom-frontend.cdx.json src/frontend

# E2E
npx --yes @cyclonedx/cyclonedx-npm \
  --output-format JSON \
  --output-file sbom/sbom-e2e.cdx.json src/tests/e2e
```

## CycloneDX 形式

CycloneDX 1.5+ JSON。主要フィールド:

- `bomFormat`, `specVersion`, `serialNumber`, `version`
- `metadata.timestamp`, `metadata.tools`, `metadata.component`
- `components[]`
  - `type` (`library` / `framework` / `application`)
  - `name`, `version`, `purl` (e.g. `pkg:nuget/Foo@1.2.3`, `pkg:npm/bar@1.0.0`)
  - `licenses[].license.id` または `licenses[].expression` (SPDX ID)
  - `externalReferences[]` (`vcs`, `website`, `issue-tracker` 等)

詳細: <https://cyclonedx.org/specification/overview/>

## 最新 SBOM の取得

1. Actions タブ → `SBOM` workflow → 最新の成功 run → Artifacts から `sbom-cyclonedx` をダウンロード。
2. リリース版は GitHub Releases ページの添付ファイルを参照。

## 注意

- SBOM はビルド成果物として **コミットしない**。`.gitignore` の `sbom/` ディレクトリは生成専用。
- ライセンスが解決できない依存があれば `oss-report.json` で `license: "UNKNOWN"` となるため、定期的に確認すること。
