---
description: 'Use when authoring or reviewing Bicep modules under infra/: CAF naming, @description / @secure decorators, latest API versions, Managed Identity, Key Vault references, Azure Verified Modules, or environment bicepparam files.'
applyTo: 'infra/**'
---

# Infrastructure (Bicep) Instructions

## モジュール構成

```
infra/
├── main.bicep
├── modules/
│   ├── network.bicep         # VNet / Private Endpoint（必要時）
│   ├── data.bicep            # SQL / Storage
│   ├── app.bicep             # SWA / Functions / Key Vault / App Configuration
│   └── observability.bicep   # App Insights / Log Analytics / Alerts
├── params/
│   ├── dev.bicepparam
│   └── prod.bicepparam
└── README.md
```

- **モジュールは単一責務**。混在させない
- リソース命名: **CAF 推奨** `{prefix}-{env}-{region}-{resource}`
  - 例: `comical-dev-jpe-swa`、`comical-prod-jpe-sql`

## 必須コーディングルール

- **すべての param に `@description('...')`** デコレータ付与
- **すべての output に `@description('...')`** デコレータ付与
- **シークレット param は `@secure()`** デコレータ付与
- **API バージョンは最新**（2026/4 時点）

```bicep
@description('Environment short code (dev/prod)')
param env string

@description('Azure region')
param location string = 'japaneast'

@secure()
@description('SQL admin password')
param sqlAdminPassword string
```

## bicepparam（環境差分）

- `params/dev.bicepparam` と `params/prod.bicepparam` を同期して維持
- 環境固有の値（SKU、リージョン、ホスト名）はここに

## モジュール参照

```bicep
module data 'modules/data.bicep' = {
  name: 'data-deployment'
  params: {
    env: env
    location: location
  }
}

resource someResource '...' = {
  // ...
  properties: {
    sqlServerId: data.outputs.sqlServerId
  }
}
```

## セキュリティ

- **シークレットは Key Vault**：bicep param に直書き禁止
- **Managed Identity** で各サービス間認証
- App Settings には **Key Vault 参照リンク**（`@Microsoft.KeyVault(SecretUri=...)`）を注入
- ストレージ・SQL は最小権限のロール割り当て

## CI 統合

- PR 時: `bicep build` + Linter + `az deployment sub what-if`
- main マージ時: `az deployment sub create` で dev に自動デプロイ
- prod は手動承認

## Azure Verified Modules

- 公式の検証済みモジュールがあれば優先利用（`br/public:avm/...`）
- カスタムモジュールはバージョンタグで参照（`latest` 禁止）

## リージョン

- **Japan East のみ**（DR 不要）
- 通貨 JPY、市場は日本国内

## アンチパターン

- ❌ ハードコードされたリソース名
- ❌ `@description()` のないパラメータ・出力
- ❌ シークレットを param に直書き
- ❌ `latest` タグでのモジュール参照
- ❌ 1 つの巨大な main.bicep にすべてを記述（モジュール分割）
- ❌ `outputs` でシークレットを返す（KV 経由で参照させる）
