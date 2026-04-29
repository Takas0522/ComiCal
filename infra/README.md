# ComiCal Infrastructure (Bicep)

This directory contains the Azure infrastructure-as-code for ComiCal
(まんがリマインダー). All Azure resources are managed declaratively with
[Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/) —
manual changes in the Portal are forbidden and will be detected by
`az deployment sub what-if` in CI.

## Layout

```
infra/
├── main.bicep                  # Subscription-scope orchestrator (creates RG)
├── modules/
│   ├── network.bicep           # Placeholder; reserved for future Private Endpoints
│   ├── data.bicep              # Azure SQL serverless + Storage account
│   ├── app.bicep               # SWA + Functions + Key Vault + App Configuration
│   └── observability.bicep     # Log Analytics + App Insights + Alerts + Workbooks
├── params/
│   ├── dev.bicepparam
│   └── prod.bicepparam
└── README.md
```

Each module has a single responsibility. `main.bicep` wires them together
and exposes deployment outputs that downstream pipelines (DACPAC publish,
backend `func publish`, frontend `swa deploy`) consume.

## Naming convention

CAF-aligned `{prefix}-{env}-{regionShort}-{resource}`. Examples for prod:

| Resource              | Name                            |
| --------------------- | ------------------------------- |
| Resource Group        | `cmcl-prod-jpe-rg`              |
| Static Web App        | `cmcl-prod-jpe-swa`             |
| Function App (API)    | `cmcl-prod-jpe-func-api`        |
| Function App (Batch)  | `cmcl-prod-jpe-func-batch`      |
| SQL Server            | `cmcl-prod-jpe-sql`             |
| SQL Database          | `cmcl-prod-jpe-sqldb`           |
| Storage Account       | `cmclprodjpest` (no dashes, ≤24)|
| Key Vault             | `cmcl-prod-jpe-kv`              |
| App Configuration     | `cmcl-prod-jpe-appcfg`          |
| Log Analytics         | `cmcl-prod-jpe-log`             |
| Application Insights  | `cmcl-prod-jpe-appi`            |

## Build / validate locally

```bash
# install bicep if missing
which bicep || az bicep install

# compile
bicep build infra/main.bicep

# preview a deployment (requires az login + an active subscription)
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/dev.bicepparam
```

## Deploy

`main.bicep` is **subscription-scoped** — it creates the resource group
itself, so deploy with `az deployment sub create`:

```bash
# dev
az deployment sub create \
  --name comical-dev-$(date +%Y%m%d-%H%M%S) \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/dev.bicepparam

# prod (gated by manual approval in CI)
az deployment sub create \
  --name comical-prod-$(date +%Y%m%d-%H%M%S) \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/params/prod.bicepparam
```

`bicepparam` files read sensitive values from environment variables:

| Env var                  | Purpose                                     |
| ------------------------ | ------------------------------------------- |
| `SQL_ADMIN_PASSWORD`     | SQL admin password (fallback only).         |
| `SQL_AAD_ADMIN_OBJECT_ID`| Entra group/user object ID for SQL admin.   |
| `SQL_AAD_ADMIN_LOGIN`    | Display name of the Entra admin.            |
| `ALERT_WEBHOOK_URL`      | Slack/Teams incoming webhook for Action Group. |

## Secrets handling

- All runtime secrets live in **Key Vault** (`cmcl-{env}-jpe-kv`) and are
  injected into App Settings as
  `@Microsoft.KeyVault(SecretUri=https://<kv>.vault.azure.net/secrets/<name>/)`.
- `app.bicep` creates **placeholder secrets** so KV references resolve
  immediately on first deploy. Replace them post-deploy:

  ```bash
  az keyvault secret set --vault-name cmcl-dev-jpe-kv \
    --name RAKUTEN-APPLICATION-ID --value '<real-value>'
  ```

- The three managed identities (SWA, Func API, Func Batch) are granted
  `Key Vault Secrets User` (RBAC) on the Key Vault. No access policies.
- SQL admin password is held in `bicepparam` only as a fallback; production
  authentication is via Entra ID + Managed Identity.

## Feature flags (App Configuration, all OFF by default)

Seeded by `app.bicep`. Toggle in the Azure Portal or via `az appconfig
feature set`:

- `discovery.recommend`
- `discovery.trending`
- `sharing.og-card`
- `sharing.public-link`
- `auth.entra-external-id`

## OIDC / Federated Credential — one-time manual prerequisite

CI (GitHub Actions) authenticates to Azure with **OIDC + Federated
Credentials**, not client secrets. Bicep deliberately does **not** manage
these because creating an Entra application requires Graph permissions
that are usually not available to the service principal performing the
infra deployment, and because the relationship between repo / branch /
environment is org-wide rather than per-resource-group.

Perform once, manually, per environment:

```bash
# 1. Create app registration & service principal
az ad app create --display-name "ComiCal-GitHub-OIDC-${ENV}"
APP_ID=$(az ad app list --display-name "ComiCal-GitHub-OIDC-${ENV}" --query '[0].appId' -o tsv)
az ad sp create --id "$APP_ID"

# 2. Assign Contributor + User Access Administrator at the subscription
az role assignment create --assignee "$APP_ID" --role Contributor \
  --scope /subscriptions/<SUB_ID>
az role assignment create --assignee "$APP_ID" --role "User Access Administrator" \
  --scope /subscriptions/<SUB_ID>

# 3. Add federated credential for the GitHub branch / environment
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-Takas0522-ComiCal-'${ENV}'",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:Takas0522/ComiCal:environment:'${ENV}'",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

Store the resulting `clientId` / `tenantId` / `subscriptionId` as GitHub
**Variables** (not secrets) at the repository or environment level. The
Stage G/Z workflow files will consume them via `azure/login@v2` with
`auth-type: IDENTITY`.

## Network

MVP runs without VNet integration to minimise cost. `network.bicep`
exists as a placeholder and exposes the `privateEndpointEnabled` switch
that future modules can react to.

## See also

- `.github/instructions/infra.instructions.md` — strict Bicep style rules
- `docs/specs/oo-init/13-infrastructure.md` — full infrastructure spec
- `docs/specs/oo-init/06-architecture-overview.md` — architecture overview
