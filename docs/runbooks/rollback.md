# ロールバック Runbook

本番障害発生時、**最短経路で前バージョンへ戻す**ための手順。

---

## 0. 共通: 障害判定

| 観測指標 | しきい値（prod） |
|---|---|
| `requests/failed` (Application Insights) | 5xx > 5% / 5min |
| `dependencies/failed` (SQL) | > 20 件 / 5min |
| smoke-test 失敗 | 1 回でも fail で即発動 |

判定したら **オンコール 1 名 + 承認者 1 名** で以下の手順を進める。

---

## 1. SWA（フロントエンド）

SWA Standard は **直前のデプロイへ即時切り戻し**できる。

```bash
RG="cmcl-prod-jpe-rg"
SWA="cmcl-prod-jpe-swa"

# 過去のデプロイ一覧
az staticwebapp environment list -g "$RG" -n "$SWA" -o table

# 1 つ前の commit SHA を再デプロイ（cd-prod.yml を workflow_dispatch で）
gh workflow run cd-prod.yml -R "$ORG/$REPO" --ref main -f sha=<前回成功 SHA>
```

> **注意**: SWA Portal の "Promote" ボタンは Preview 環境向けで、本番への
> 即時切り戻しは「前回 SHA を再 deploy する」ほうが Bicep / DACPAC との
> 整合性が取れる。

## 2. Functions（API / Batch）

slot 運用していれば **swap で即時ロールバック**。

```bash
FUNC_API="cmcl-prod-jpe-func-api"

# staging に問題のあるバージョンが残っているなら swap し戻す
az functionapp deployment slot swap \
  -g "$RG" -n "$FUNC_API" \
  --slot staging --target-slot production
```

slot 運用していない場合：

```bash
# 過去 zip パッケージから再 deploy
az functionapp deployment source config-zip \
  -g "$RG" -n "$FUNC_API" \
  --src ./previous-api.zip
```

> 過去 artifact は GitHub Actions の `prod-artifacts` から 30 日間入手可能。

## 3. SQL Database (DACPAC)

DACPAC は冪等で **前バージョンを再 publish すれば差分のみ戻る**。
ただし、データ削除を伴うスキーマ変更は手動の事前確認が必須。

```bash
git checkout <前回成功 SHA> -- src/db
dotnet build src/db/ComiCal.Database.sqlproj -c Release

ACCESS_TOKEN=$(az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv)
sqlpackage \
  /Action:Publish \
  /SourceFile:./src/db/bin/Release/ComiCal.Database.dacpac \
  /TargetServerName:cmcl-prod-jpe-sql.database.windows.net \
  /TargetDatabaseName:cmcl-prod-jpe-sqldb \
  /AccessToken:"$ACCESS_TOKEN" \
  /p:BlockOnPossibleDataLoss=true \
  /p:DropObjectsNotInSource=false
```

### 3.1 データ起因の事故（誤 UPDATE / 誤 DELETE）

DACPAC では復旧できない。**Azure SQL Point-in-Time Restore** を使う。

```bash
NEW_DB="cmcl-prod-jpe-sqldb-restore-$(date -u +%Y%m%d%H%M)"
az sql db restore \
  -g "$RG" -s cmcl-prod-jpe-sql \
  -n cmcl-prod-jpe-sqldb \
  --dest-name "$NEW_DB" \
  --time "<事故前の UTC タイムスタンプ>"
# 確認後、本体 DB と差し替え（要メンテ枠）
```

## 4. Bicep（インフラ構成変更の切り戻し）

```bash
git checkout <前回成功 SHA> -- infra/
gh workflow run cd-prod.yml -R "$ORG/$REPO" --ref main -f sha=<前回成功 SHA>
```

> Bicep what-if が **削除を伴う差分**を出した場合は必ず承認者と確認。
> Resource Group 全削除のような diff が出る場合は **絶対に apply しない**。

## 5. 完全リカバリ手順（最悪ケース）

1. 直前成功 SHA の `cd-prod.yml` を workflow_dispatch で再実行
2. smoke-test 失敗が継続するなら一段前の SHA で再試行
3. それでも回復しないなら IaC のコード（feature flag）で機能を OFF
   → `infra/modules/app.bicep` の `featureFlags` を編集して deploy

## 6. ポストモーテム

- 24 時間以内に Issue を立てる（テンプレート: `bug + postmortem`）
- 5 Whys + 修正 PR + テスト追加（再発防止）
- ADR が必要なら `docs/adr/` に追加

## 7. 関連

- `docs/runbooks/cd-prod-setup.md`
- `docs/specs/oo-init/14-observability-sre.md`
- `docs/specs/oo-init/18-operations.md`（存在する場合）
