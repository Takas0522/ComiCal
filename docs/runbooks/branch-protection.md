# GitHub Branch Protection 設定 Runbook

`main` ブランチへの強制マージを防ぐため、リポジトリ管理者が以下の
**Required status checks** と **Required reviews** を設定する。

---

## 1. Settings → Branches → Branch protection rules

`main` に対して以下を有効化する。

- ✅ **Require a pull request before merging**
  - Required approvals: **1**
  - Dismiss stale approvals when new commits are pushed
  - Require review from Code Owners
- ✅ **Require status checks to pass before merging**
  - Require branches to be up to date before merging
  - 必須チェック（下記 2 を参照）
- ✅ **Require conversation resolution before merging**
- ✅ **Require signed commits**（推奨）
- ✅ **Require linear history**
- ✅ **Do not allow bypassing the above settings**
- ✅ **Restrict who can push to matching branches**: 管理者のみ
- ❌ **Allow force pushes** — OFF
- ❌ **Allow deletions** — OFF

## 2. 必須 Status Checks（`ci.yml` ジョブ名で登録）

| Job 名 | 内容 |
|---|---|
| `Lint (frontend)` | ESLint + Prettier |
| `Test (frontend)` | Jest（coverage ≥ 80% は `jest.config.ts` の `coverageThreshold` で強制）|
| `Build (frontend SSR)` | Angular SSR ビルド |
| `Format (backend)` | `dotnet format --verify-no-changes` |
| `Build (backend)` | `dotnet build -warnaserror` |
| `Build (DB DACPAC)` | sqlproj build |
| `Test (backend unit)` | xUnit unit。`coverage.runsettings` の `<Threshold>80</Threshold>` で 80% gate |
| `Test (backend integration)` | xUnit integration（Testcontainers）|
| `Bicep build (lint)` | bicep build / build-params |
| `CodeQL (csharp)` | CodeQL（critical/high で fail）|
| `CodeQL (javascript-typescript)` | 同上 |

> `Bicep what-if (dev)` `Bicep what-if (prod)` は `vars.AZURE_TENANT_ID`
> が無いと skip されるため**任意**チェックとして登録する（required にすると
> setup 完了前にマージブロックが起きる）。

## 3. CodeQL の重大度フィルタ

GitHub Settings → Code security and analysis → **Code scanning default setup** で：

- Severity threshold: **High or above**
- Failures on PR: **block merge**

## 4. Environment Protection

| Environment | Required reviewers | Branch limits |
|---|---|---|
| `dev` | 0（自動） | `main` |
| `production` | 1 名以上 | `main` または `refs/tags/v*` |

## 5. CODEOWNERS

`.github/CODEOWNERS` で各ディレクトリの自動レビュー割当を行う。最低限：

```
# Default
*       @Takas0522

# IaC は SRE
infra/  @Takas0522

# 本番 deploy 用 workflow は管理者承認必須
.github/workflows/cd-prod.yml @Takas0522
```

## 6. 確認方法

```bash
gh api repos/Takas0522/ComiCal/branches/main/protection \
  --jq '.required_status_checks.contexts'
```

期待値: 上記 §2 のリストが含まれていること。

## 7. 関連

- `docs/runbooks/cd-dev-setup.md`
- `docs/runbooks/cd-prod-setup.md`
- `docs/specs/oo-init/17-cicd.md` §17.4 PR 品質ゲート
