#!/usr/bin/env bash
# ComiCal CI/CD bootstrap — Entra ID app registration, Federated Credentials,
# GitHub Environments, repository/environment Secrets.
#
# Idempotent: safe to re-run. Reads existing resources before creating.
#
# Prerequisites: az login, gh auth login (with `repo` scope)
set -euo pipefail

REPO="Takas0522/ComiCal"
APP_NAME="comical-github-oidc"
SUB_ID="$(az account show --query id -o tsv)"
TENANT_ID="$(az account show --query tenantId -o tsv)"

echo "Subscription: $SUB_ID"
echo "Tenant:       $TENANT_ID"
echo "Repo:         $REPO"
echo "App name:     $APP_NAME"
echo "----"

# ---------- 1. Entra ID application + service principal ----------
APP_ID="$(az ad app list --display-name "$APP_NAME" --query '[0].appId' -o tsv)"
if [[ -z "$APP_ID" ]]; then
  echo "Creating Entra ID app: $APP_NAME"
  APP_ID="$(az ad app create --display-name "$APP_NAME" --sign-in-audience AzureADMyOrg --query appId -o tsv)"
else
  echo "Reusing app: $APP_ID"
fi
echo "AppId: $APP_ID"

SP_ID="$(az ad sp list --filter "appId eq '$APP_ID'" --query '[0].id' -o tsv)"
if [[ -z "$SP_ID" ]]; then
  echo "Creating service principal"
  SP_ID="$(az ad sp create --id "$APP_ID" --query id -o tsv)"
else
  echo "Reusing SP: $SP_ID"
fi

# ---------- 2. Role assignments (Contributor + UAA on subscription) ----------
SCOPE="/subscriptions/$SUB_ID"
for ROLE in "Contributor" "User Access Administrator"; do
  EXISTS="$(az role assignment list --assignee "$APP_ID" --scope "$SCOPE" --role "$ROLE" --query '[0].id' -o tsv 2>/dev/null || true)"
  if [[ -z "$EXISTS" ]]; then
    echo "Granting role: $ROLE"
    az role assignment create --assignee-object-id "$SP_ID" --assignee-principal-type ServicePrincipal \
      --role "$ROLE" --scope "$SCOPE" >/dev/null
  else
    echo "Role already assigned: $ROLE"
  fi
done

# ---------- 3. Federated Credentials (4 subjects) ----------
declare -a FCS=(
  "gh-env-dev|repo:$REPO:environment:dev"
  "gh-env-prod|repo:$REPO:environment:prod"
  "gh-branch-main|repo:$REPO:ref:refs/heads/main"
  "gh-pull-request|repo:$REPO:pull_request"
)
for entry in "${FCS[@]}"; do
  NAME="${entry%%|*}"
  SUBJECT="${entry##*|}"
  EXISTS="$(az ad app federated-credential list --id "$APP_ID" --query "[?name=='$NAME'].name" -o tsv)"
  if [[ -z "$EXISTS" ]]; then
    echo "Creating federated credential: $NAME"
    az ad app federated-credential create --id "$APP_ID" --parameters "{
      \"name\":\"$NAME\",
      \"issuer\":\"https://token.actions.githubusercontent.com\",
      \"subject\":\"$SUBJECT\",
      \"audiences\":[\"api://AzureADTokenExchange\"]
    }" >/dev/null
  else
    echo "Federated credential exists: $NAME"
  fi
done

# ---------- 4. GitHub Environments (dev / prod) ----------
for ENV in dev prod; do
  echo "Ensuring environment: $ENV"
  gh api -X PUT "repos/$REPO/environments/$ENV" --silent
done

# ---------- 5. Required reviewer on prod ----------
USER_ID="$(gh api users/Takas0522 --jq .id)"
echo "Setting required reviewer on prod (user_id=$USER_ID)"
printf '{"wait_timer":0,"reviewers":[{"type":"User","id":%d}],"deployment_branch_policy":null}' "$USER_ID" \
  | gh api -X PUT "repos/$REPO/environments/prod" --input - --silent

# ---------- 6. Generate strong SQL passwords ----------
gen_pwd() {
  # 32 chars, satisfies SQL complexity (upper+lower+digit+symbol).
  # openssl rand -base64 produces upper/lower/digit/+,/,= which SQL accepts.
  # Prefix "Aa1!" to guarantee complexity classes regardless of random output.
  printf 'Aa1!%s' "$(openssl rand -base64 24 | tr -d '\n=/+' | cut -c1-28)"
}
SQL_PWD_DEV="$(gen_pwd)"
SQL_PWD_PROD="$(gen_pwd)"

# ---------- 7. Set repository + environment secrets ----------
# Repository-wide
gh secret set AZURE_TENANT_ID       --repo "$REPO" --body "$TENANT_ID"
gh secret set AZURE_CLIENT_ID       --repo "$REPO" --body "$APP_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo "$REPO" --body "$SUB_ID"

# Environment-scoped (dev)
gh secret set SQL_ADMIN_PASSWORD --repo "$REPO" --env dev  --body "$SQL_PWD_DEV"
gh secret set AZURE_CLIENT_ID    --repo "$REPO" --env dev  --body "$APP_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo "$REPO" --env dev --body "$SUB_ID"

# Environment-scoped (prod)
gh secret set SQL_ADMIN_PASSWORD --repo "$REPO" --env prod --body "$SQL_PWD_PROD"
gh secret set AZURE_CLIENT_ID    --repo "$REPO" --env prod --body "$APP_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo "$REPO" --env prod --body "$SUB_ID"

# Placeholder SWA tokens (filled in after first IaC deploy)
gh secret set SWA_DEPLOY_TOKEN --repo "$REPO" --env dev  --body "PLACEHOLDER_FILL_AFTER_FIRST_DEPLOY"
gh secret set SWA_DEPLOY_TOKEN --repo "$REPO" --env prod --body "PLACEHOLDER_FILL_AFTER_FIRST_DEPLOY"
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN_DEV --repo "$REPO" --body "PLACEHOLDER_FILL_AFTER_FIRST_DEPLOY"

cat <<EOF

============================================================
DONE.
  AppId (AZURE_CLIENT_ID): $APP_ID
  TenantId:                $TENANT_ID
  SubscriptionId:          $SUB_ID

  Federated Credentials registered:
    - environment:dev
    - environment:prod
    - ref:refs/heads/main
    - pull_request

  GitHub Environments: dev, prod (prod has Takas0522 as required reviewer)

  Secrets set:
    Repo: AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_SUBSCRIPTION_ID,
          AZURE_STATIC_WEB_APPS_API_TOKEN_DEV (placeholder)
    dev:  SQL_ADMIN_PASSWORD, AZURE_CLIENT_ID, AZURE_SUBSCRIPTION_ID,
          SWA_DEPLOY_TOKEN (placeholder)
    prod: SQL_ADMIN_PASSWORD, AZURE_CLIENT_ID, AZURE_SUBSCRIPTION_ID,
          SWA_DEPLOY_TOKEN (placeholder)

  Next steps after first 'CD - Dev' run succeeds:
    SWA_TOKEN=\$(az staticwebapp secrets list -g cmcl-dev-jpe-rg -n <swa-name> --query properties.apiKey -o tsv)
    gh secret set SWA_DEPLOY_TOKEN --repo $REPO --env dev --body "\$SWA_TOKEN"
    gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN_DEV --repo $REPO --body "\$SWA_TOKEN"
============================================================
EOF
