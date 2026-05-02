---
name: add-bicep-module
description: 'Use when adding a new Azure resource module to IaC (Storage, Functions, SWA, SQL, Key Vault, App Configuration, App Insights, etc.) under infra/modules/. Enforces CAF naming, @description on params/outputs, @secure for secrets, latest API versions, Managed Identity, and Azure Verified Module preference.'
argument-hint: '<moduleName> <purpose>'
allowed-tools: Read, Write, Edit, Bash
---

# add-bicep-module

## 配置

- `infra/modules/<name>.bicep`
- 環境差分: `infra/params/dev.bicepparam`, `infra/params/prod.bicepparam`

## 必須要件

1. **CAF 命名**: `{prefix}-{env}-{region}-{resource}`
2. **すべての param に `@description()`**
3. **すべての output に `@description()`**
4. **シークレット param は `@secure()`**
5. **API バージョンは最新**（2026/4 時点を確認）
6. **Managed Identity** を優先、接続文字列ベース禁止
7. **シークレットは Key Vault 経由**、output で返さない

## テンプレート

```bicep
@description('Environment short code (dev/prod)')
param env string

@description('Azure region')
param location string = 'japaneast'

@description('Common prefix for resource naming')
param prefix string = 'comical'

var name = '${prefix}-${env}-jpe-storage'

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: replace(name, '-', '')
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  identity: { type: 'SystemAssigned' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

@description('Storage account resource ID')
output storageAccountId string = storage.id

@description('Storage account name')
output storageAccountName string = storage.name
```

## 検証

```bash
az bicep build --file infra/modules/<name>.bicep
az bicep lint --file infra/modules/<name>.bicep
az deployment sub what-if --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/dev.bicepparam
```

## main.bicep への統合

```bicep
module myMod 'modules/<name>.bicep' = {
  name: '<name>-deployment'
  params: {
    env: env
    location: location
    prefix: prefix
  }
}
```

## チェックリスト

- [ ] CAF 命名規約に従う
- [ ] 全 param/output に `@description()`
- [ ] シークレットは `@secure()` + Key Vault 参照
- [ ] API バージョン最新
- [ ] Managed Identity 設定
- [ ] `az bicep build` 成功
- [ ] `what-if` で意図通りのプラン
- [ ] Azure Verified Module で代替できないか確認

## 関連

- `.github/instructions/infra.instructions.md`
- テンプレート: `templates/module.template.bicep`

## アンチパターン

- ❌ ハードコード命名
- ❌ `@description()` 抜け
- ❌ シークレットを `output` で返す
- ❌ 接続文字列ベースの認証
- ❌ `latest` タグでの AVM 参照
