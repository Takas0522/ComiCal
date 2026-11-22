# 0004. Azure Cost Reduction: Single Environment, Local Batch, No Static Egress

- **Status**: Accepted
- **Date**: 2026-11-21
- **Deciders**: Repository maintainer

## Context and Problem Statement

ComiCal is a personal-use application, but its Azure bill reached about 33,500 JPY/month in 2026-10
(Stage/dev ≈ 12,800, prod ≈ 20,600). The breakdown (Cost Management, see `plan/azure-cost-reduction.md`)
showed that almost all of it was fixed cost unrelated to actual usage:

- A full Stage (dev) environment, including its own NAT Gateway and SQL database.
- App Configuration **Standard** (≈ 5,900 JPY/month) that no code reads (no SDK, three disabled flags).
- A NAT Gateway + static Public IP per environment (≈ 5,900 JPY/month each). The Rakuten Web Service API,
  migrated in 2026-05, requires the caller to be allowlisted; backend (server) callers are matched by
  source IP, so every Azure-hosted caller (batch **and** API) needed a static egress IP.
- SQL Serverless with `minCapacity = 1.0` and `autoPauseDelay = 60`. CPU usage is a few percent, so the
  bill was "1 vCore × time awake" (≈ 1.5–2.5 h/day) plus storage billed on the configured 32 GB max size.

## Decision Drivers

- Monthly cost first; availability/SLA is secondary for a personal-use app.
- Keep the codebase and the deployed state in sync (IaC remains the source of truth).
- Minimise application code changes.
- Stay within the Rakuten Web Service terms (no spoofed Referer/Origin from servers).

## Considered Options

- **Option A: Single prod environment, batch on the maintainer's PC, no static egress in Azure** —
  retire Stage, delete App Configuration, run the Durable Functions batch locally (Windows Task Scheduler
  + Azurite), disable the API's Rakuten fallback, remove VNet/NAT/PIP, tune SQL serverless and move it to
  the SQL Database free offer.
- **Option B: Keep the batch in Azure, only trim SKUs** — still needs NAT Gateway for the Rakuten IP
  allowlist; saves far less.
- **Option C: Run the batch on GitHub Actions (free for public repos)** — runner IPs cannot be
  allowlisted at Rakuten; not viable.
- **Option D (Status quo)**.

## Decision Outcome

採用案: **Option A**

- Stage (dev) is retired. Validation happens locally (DevContainer / docker compose); prod deploys stay
  tag-driven with What-If.
- App Configuration is removed. If feature flags are needed later, use the Free (one per subscription)
  or Developer tier.
- The batch (`src/backend/batch`) is no longer deployed. `tools/run-batch-local.ps1` runs it once
  (Azurite task hub, prod SQL/Blob via `az login`, Rakuten secrets from Key Vault);
  `tools/register-batch-task.ps1` schedules it daily. The maintainer's home IP is allowlisted at Rakuten.
- The API no longer calls Rakuten (decision **D-1**): its Rakuten settings are not configured, so
  `NullRakutenBookSearchService` is used. Search returns DB results only; "subscribe from Rakuten"
  candidates are not shown. The batch collects releases from 6 months back to 6 months ahead daily.
- VNet / NAT Gateway / Public IP are removed (`infra/modules/network.bicep` deleted).
- SQL: `minCapacity 0.5`, `autoPauseDelay 15`, free offer (`useFreeLimit`, `BillOverUsage`).
- SWA stays on Standard: Free cannot link an existing Functions app or use custom auth registrations.
- A resource-group budget (3,000 JPY/month) notifies the existing action group.

### Consequences

- ✅ Positive: Expected cost drops from ≈ 33,500 to ≈ 1,500–2,500 JPY/month.
- ✅ Positive: Fewer moving parts in Azure (one Function App, no networking).
- ⚠️ Negative / Trade-off: The batch depends on the maintainer's PC being on (missed runs execute on
  next start) and on a stable home IP for the Rakuten allowlist.
- ⚠️ Negative / Trade-off: Titles not yet in the DB cannot be found/subscribed via Rakuten from the UI.
  A browser-side Rakuten call (web-application type app) is a possible future option (D-2).
- ⚠️ Negative / Trade-off: The free-offer database has no SLA and 7-day PITR.
- ⚠️ Negative / Trade-off: No pre-production environment in Azure.

## Validation

- `az resource list -g cmcl-prod-jpe-rg` contains no VNet/NAT/PIP, batch Function App or App Configuration.
- `az sql db show` reports `useFreeLimit: true`, `minCapacity: 0.5`, `autoPauseDelay: 15`.
- `BatchRuns` shows a successful daily run from the local batch (`/admin/batch-runs`).
- Cost Management shows the resource group under the 3,000 JPY budget the month after rollout.

## Links

- `plan/azure-cost-reduction.md` (analysis and phased plan)
- `plan/scripts/apply-0*.ps1` (Azure-side changes matching this IaC)
- Azure SQL Database free offer: https://learn.microsoft.com/azure/azure-sql/database/free-offer

## Update (2026-11-22): Console Batch

### Considered Options

- Keep the original local Durable Functions arrangement. It requires Functions Core Tools, an Azurite task hub and a PowerShell launcher.
- Use the Durable Task SDK with Durable Task Scheduler Consumption. The Japan East retail price reviewed for the decision could be read as **JPY 0.688 per action**. At roughly 200,000 actions per month, that interpretation implies about JPY 137,000 per month. The pricing risk ruled out this option.
- Run a .NET 10 Generic Host console application directly from Windows Task Scheduler.

### Decision Outcome

Use the Generic Host console application. `tools/publish-batch.ps1` deploys the executable to the maintainer's PC, and `tools/register-batch-task.ps1` schedules `ComiCal.Batch.exe run` at 03:00 JST. Jobs use scoped Activities for SQL, Blob and Rakuten operations. Azure CLI credentials and Key Vault continue to supply access to production resources.

### Consequences

- After an interruption the next run starts from the beginning. ISBN upserts and cover hash comparisons support idempotent reprocessing. Stale cleanup marks abandoned Running records as Failed when they exceed the configured age.
- A successful run for the same JST day skips collection unless forced. A run that reaches the end with item failures returns 0 and logs a warning while its database status is Failed. With no Succeeded record for the day, a manual `run` reprocesses the full collection.
- Only `run` results can send a failure webhook or heartbeat. The webhook includes a Slack / Teams compatible `text` field. The external heartbeat also detects missed runs.

### Validation

Use `preflight` to verify connectivity and `status` to verify the recorded outcome. Confirm Windows task execution and receipt of the external heartbeat during rollout. See the [Batch operations runbook](../runbooks/batch-operations.md).
