# cd-prod セットアップ Runbook

`cd-prod.yml` を本番運用するための初期設定を記載する。dev とは Azure
サブスクリプションを分離し、GitHub Environment 保護で承認制にする。

---

## 1. 前提

- dev 設定（`cd-dev-setup.md`）が完了している
- 本番用 Azure サブスクリプションが分離されている（推奨）
- Approver 1 名以上が指名されている

## 2. Azure 側の準備

### 2.1 サービスプリンシパル（prod 用）

```bash
APP_NAME_PROD="comical-cd-prod"
SUB_ID_PROD="<prod サブスクリプション ID>"

APP_ID_PROD=$(az ad app create --display-name "$APP_NAME_PROD" --query appId -o tsv)
az ad sp create --id "$APP_ID_PROD"
SP_OBJ_ID_PROD=$(az ad sp show --id "$APP_ID_PROD" --query id -o tsv)

az role assignment create --assignee "$APP_ID_PROD" \
  --role Contributor --scope "/subscriptions/$SUB_ID_PROD"
az role assignment create --assignee "$APP_ID_PROD" \
  --role "User Access Administrator" --scope "/subscriptions/$SUB_ID_PROD"
```

### 2.2 Federated Credential（Environment `production`）

```bash
ORG="Takas0522"
REPO="ComiCal"
ENV="production"

az ad app federated-credential create \
  --id "$APP_ID_PROD" \
  --parameters '{
    "name": "comical-prod-environment",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$ORG/$REPO"':environment:'"$ENV"'",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

> dev とは別の credential。**dev のサービスプリンシパルが prod に届かない**
> よう role assignment を完全に分離する。

## 3. GitHub Environment `production`

`Settings → Environments → New environment` で `production` を作成し、
**保護ルールを必ず**有効化する：

- ✅ **Required reviewers**: 最低 1 名（責任者）
- ✅ **Wait timer**: 0 分（必要なら 5 分）
- ✅ **Deployment branches**: `main` または `Selected ref` で `v*` タグのみ
- ✅ **Prevent self-review** を ON

### 3.1 Variables（Repository scope に追記、または Environment scope）

| 名前 | 値 |
|---|---|
| `AZURE_CLIENT_ID` | prod 用 SPN の `$APP_ID_PROD`（**Environment scope** に上書き） |
| `AZURE_SUBSCRIPTION_ID` | prod サブスクリプション ID（**Environment scope**） |
| `AZURE_TENANT_ID` | テナント ID（dev と共通なら Repository scope のまま） |
| `SQL_AAD_ADMIN_OBJECT_ID` | `$SP_OBJ_ID_PROD` |
| `SQL_AAD_ADMIN_LOGIN` | `comical-cd-prod` |
| `FUNCTIONS_API_SLOT_PROD` | （任意）`staging` を指定すると staging slot にデプロイ → swap 運用 |

### 3.2 Environment Secrets

| 名前 | 用途 |
|---|---|
| `SQL_ADMIN_PASSWORD` | prod SQL 管理者パスワード（強パスワード）|
| `ALERT_WEBHOOK_URL` | Action Group の通知 Webhook |

## 4. リリースフロー

### 4.1 通常リリース（タグ駆動）

```bash
# main が dev で smoke 通過済みであることを確認
gh release create v1.2.3 --target main \
  --title "v1.2.3" \
  --notes "$(git log --pretty=format:'%h %s' v1.2.2..main)"
# → release publish イベントで cd-prod.yml が起動
# → preflight が「同 SHA で cd-dev が成功している」ことを確認
# → production environment の必須レビュアーへ承認依頼が飛ぶ
```

### 4.2 緊急 / 手動リリース（workflow_dispatch）

```bash
gh workflow run cd-prod.yml -R "$ORG/$REPO" \
  --ref main -f sha=<commit SHA>
```

`sha` を省略すると workflow が実行された ref の HEAD を使う。

## 5. 承認者の手元での確認事項

承認 UI に進む前に：

- [ ] `cd-dev` が同 SHA で **成功**している（preflight ジョブで自動確認されるが、目視も）
- [ ] PR レビューが完了している（Conventional Commits / 80% 以上のカバレッジ / CodeQL クリーン）
- [ ] Bicep what-if の差分を確認（resource group / SKU の意図せぬ変更がないか）
- [ ] dev 環境で 1 日以上動作観察済み（バッチ夜間実行、アラート未発火）

## 6. デプロイ後の確認

- [ ] smoke-test ジョブが green
- [ ] Application Insights の Live Metrics でエラー率が dev と同等
- [ ] `annotate-release` ジョブで Release notes に追記された

## 7. ロールバック

`docs/runbooks/rollback.md` を参照。

## 8. 関連

- `docs/runbooks/cd-dev-setup.md`
- `docs/runbooks/branch-protection.md`
- `docs/specs/oo-init/17-cicd.md` §17.6 リリースフロー
