---
name: update-openapi
description: 'Use when a backend API change requires updating the OpenAPI spec: new endpoint added, request/response schema modified, error response added, or version bump. Keeps openapi.yaml, Functions OpenAPI annotations, RFC 7807 Problem schemas, and frontend generated client in sync; runs redocly lint.'
argument-hint: '<endpointOrSchemaName>'
allowed-tools: Read, Write, Edit, Bash
---

# update-openapi

## 配置

- 仕様書: `src/backend/api/ComiCal.Api/openapi.yaml`（または `.json`）
- 生成されたクライアント: `src/frontend/src/api/generated/`（必要時）

## 手順

1. **エンドポイント変更を反映**
   - `paths.<route>.<method>` に summary / description / parameters / requestBody / responses を記述
   - エラーは **`application/problem+json`** スキーマで（RFC 7807）

2. **共通スキーマは `components/schemas/`**
   - `Problem`（RFC 7807 共通）
   - `ValidationProblem`（FluentValidation 対応）
   - `Subscription`、`Volume`、`Series`、`Purchase` 等

3. **セキュリティ定義**
   - Function Key + Entra ID 両方
   - `security` をエンドポイントごと/全体で明示

4. **バージョニング**
   - `info.version` を SemVer 互換で更新
   - 破壊的変更は major bump

5. **検証**
   ```bash
   npx @redocly/cli lint src/backend/api/ComiCal.Api/openapi.yaml
   ```

6. **フロント生成**（必要なら）
   ```bash
   pnpm openapi:generate
   ```
   - 生成物は手動編集禁止、変更時は仕様書を変更

## チェックリスト

- [ ] 全エンドポイントに `summary` と `operationId`
- [ ] `4xx` には Problem スキーマ
- [ ] `requestBody` の必須フィールドが `required` に列挙
- [ ] バージョンが上がっている
- [ ] lint が pass
- [ ] フロント側型と整合

## 関連

- `.github/instructions/backend-api.instructions.md`
- `add-functions-endpoint` Skill

## アンチパターン

- ❌ 仕様書だけ更新して実装が追従していない（または逆）
- ❌ エラーレスポンスを inline スキーマで重複定義
- ❌ 生成クライアントを手動編集
- ❌ 破壊的変更を patch / minor で出す
