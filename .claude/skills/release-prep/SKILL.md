---
name: release-prep
description: 'Use only when explicitly preparing a production release (before creating a git tag). Walks through CI green check, version bumps (frontend/backend/OpenAPI), CHANGELOG (Keep a Changelog), ADR status sync, Bicep what-if for dev/prod, DACPAC drift check, tag + GitHub Release notes, and rollback plan. Auto-invocation disabled to require human confirmation.'
disable-model-invocation: true
allowed-tools: Bash, Read, Write, Edit
---

# release-prep

## 概要

本番リリースは手動承認が必要。**人間の確認なしに勝手に走らせない**ため `disable-model-invocation: true`。明示的に呼ばれた場合のみ実行する。

## チェックリスト

### 1. ブランチ・履歴
- [ ] `main` が green（CI 全成功）
- [ ] 過去 1 リリースからの差分を `git log <prev-tag>..HEAD --oneline` で確認

### 2. バージョン更新
- [ ] フロント `package.json` `version`
- [ ] バックエンド `*.csproj` `<Version>`
- [ ] OpenAPI `info.version`
- [ ] Bicep / IaC のバージョン参照（あれば）

### 3. CHANGELOG
- [ ] `CHANGELOG.md` を Keep a Changelog 形式で更新
  - Added / Changed / Fixed / Deprecated / Removed / Security
- [ ] 関連 PR 番号と Issue を列挙

### 4. ADR 整合
- [ ] 当該リリースに含まれる ADR の Status を `Accepted` に
- [ ] Superseded 関係があれば双方向リンク

### 5. インフラ what-if
- [ ] dev: `az deployment sub what-if --parameters infra/params/dev.bicepparam`
- [ ] prod: `az deployment sub what-if --parameters infra/params/prod.bicepparam`
- [ ] 想定外の差分がないこと

### 6. データベース drift
- [ ] `sqlpackage /Action:DeployReport` で破壊的変更（`BlockOnPossibleDataLoss`）が出ないこと
- [ ] 必要な PreDeploy / PostDeploy が含まれること

### 7. タグとリリースノート
- [ ] `git tag -a vX.Y.Z -m "..."`
- [ ] GitHub Release ノートを CHANGELOG から生成
- [ ] アーティファクトのチェックサム公開

### 8. 監視・ロールバック計画
- [ ] App Insights / Slack 通知が有効
- [ ] ロールバック手順を Release ノートに明記（前バージョンへの再デプロイ）

## 関連

- `.github/instructions/iac-pipeline.instructions.md`
- `update-openapi` Skill
- `write-adr` Skill

## アンチパターン

- ❌ what-if を確認せずタグ push
- ❌ CHANGELOG 未更新
- ❌ DACPAC drift 無視
- ❌ ロールバック手順なし
- ❌ 主要 PR を CHANGELOG から漏らす
