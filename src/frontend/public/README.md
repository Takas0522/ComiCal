# Frontend public assets

Files under this directory are copied as-is to the SSR/SPA root by the Angular
builder (configured via `angular.json` → `assets[].input: "public"`).

## `oss-report.json`

A flat array of `{ name, version, license, url }` consumed by the `/legal/oss`
page and the in-app OSS dialog.

> ⚠️ This file is a **placeholder** for local development. The real artefact is
> generated in CI by `tools/oss-report/generate-oss-report.sh` from CycloneDX
> SBOM JSON files (frontend + backend) and committed monthly via PR.
