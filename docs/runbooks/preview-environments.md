# PR Preview Environment Runbook

`pr-preview.yml` の動作・制限・トラブルシュート。

---

## 1. 概要

- SWA Standard の **Preview Environment** 機能を `Azure/static-web-apps-deploy@v1`
  で利用し、PR ごとに `https://<swa>-pr-<N>.<region>.azurestaticapps.net`
  形式の URL を作る。
- PR が **同一リポジトリのブランチ**から作られた場合のみ動く（fork PR は
  secrets / OIDC が使えないため除外）。
- PR `closed` イベントで自動破棄。

## 2. 構成（共有方針）

| 層 | Preview 専用？ | 共有先 |
|---|---|---|
| Frontend (SWA) | ✅ PR ごと | — |
| Backend Functions API | ❌ 共有 | **dev** |
| Backend Functions Batch | ❌ 共有 | **dev** |
| SQL DB | ❌ 共有 | **dev** |
| Blob Storage | ❌ 共有 | **dev** |
| Entra External ID | ❌ 共有 | **dev** |

### なぜ Functions / DB を共有するか

- 1 PR ごとに Function App + SQL Database + Blob を作るとコストが膨らむ
- バックエンドのスキーマ変更を含む PR は CI 時点で DACPAC build で検出できる
- フロントエンドのみの PR が大半で、バックエンドが関わる PR は**少数**

### リスクと緩和

| リスク | 緩和 |
|---|---|
| PR が dev DB に書き込み、他 PR と干渉 | Preview ユーザーは未ログインで読み取り中心。書き込みは Phase 2 以降のため、Phase 1 中はリスク極小 |
| 楽天 API キーが PR で漏れる | dev SWA は dev 環境変数のみ参照。PR 経由で secrets が露出することはない |
| fork PR の secrets 露出 | `if: github.event.pull_request.head.repo.full_name == github.repository` で fork を除外 |
| PR で破壊的 DACPAC を入れた | `cd-dev.yml` でしか DACPAC は publish されない。preview は frontend のみ |

## 3. URL の確認

PR コメントに自動投稿される `🔍 Preview deployed: <URL>` をクリックする。
`<!-- comical-pr-preview -->` マーカーで同一コメントを更新する（複数投稿を避ける）。

## 4. 手動で破棄したいとき

通常は PR close で自動破棄されるが、ハングした場合：

```bash
RG="cmcl-dev-jpe-rg"
SWA="cmcl-dev-jpe-swa"
PR=42

az staticwebapp environment delete -g "$RG" -n "$SWA" \
  --environment-name "pr-$PR" --yes
```

## 5. fork PR を試したい場合

CODEOWNERS 承認の上で、メンテナがフォーク commit を fetch してから
**自分のフォークでない feature branch に push し直し**、そこから PR を
作り直す。fork のままの PR には preview を付けない方針。

## 6. トラブルシュート

| 症状 | 対処 |
|---|---|
| Preview URL が空 | `Azure/static-web-apps-deploy` の出力 `static_web_app_url` が空。先に dev 本体に成功 deploy が要る（環境が無いとプレビューも作れない）|
| `secrets list` が `Forbidden` | dev SPN に `Contributor` か `Static Web App Contributor` 権限があるか確認 |
| 古い preview が残る | 5. の手動破棄手順 |

## 7. 関連

- `docs/runbooks/cd-dev-setup.md`
- `docs/specs/oo-init/17-cicd.md` §17.8
