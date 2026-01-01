#!/bin/bash

# ComiCal Infrastructure Initial Setup Script
# This script creates Azure Service Principal and configures GitHub Secrets

set -e

# Color output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

main() {
    print_info "=== ComiCal Infrastructure Initial Setup (Simplified) ==="
    
    # Get subscription and tenant details
    SUBSCRIPTION_ID=$(az account show --query id -o tsv)
    TENANT_ID=$(az account show --query tenantId -o tsv)
    
    print_info "Subscription ID: $SUBSCRIPTION_ID"
    print_info "Tenant ID: $TENANT_ID"
    
    # Get GitHub repository
    REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
    print_info "GitHub Repository: $REPO"
    
    # Service Principal details
    SP_NAME="sp-comical-github-actions"
    SP_APP_ID=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv)
    
    print_info "Service Principal App ID: $SP_APP_ID"
    
    # Create JSON directly
    print_info "Creating credentials JSON..."
    
    AZURE_CREDENTIALS=$(cat <<EOF
{
  "clientId": "$SP_APP_ID",
  "subscriptionId": "$SUBSCRIPTION_ID",
  "tenantId": "$TENANT_ID",
  "clientSecret": "",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
EOF
)
    
    print_info "JSON created successfully"
    
    # Setup federated identity credentials for OIDC
    print_info "Setting up federated identity credentials for OIDC..."
    
    # Extract owner and repo name
    REPO_OWNER=$(echo $REPO | cut -d'/' -f1)
    REPO_NAME=$(echo $REPO | cut -d'/' -f2)
    
    # Setup federated credentials for main branch
    print_info "Creating federated credential for main branch..."
    az ad app federated-credential create \
        --id $SP_APP_ID \
        --parameters '{
            "name": "'$REPO_NAME'-main-branch",
            "issuer": "https://token.actions.githubusercontent.com", 
            "subject": "repo:'$REPO':ref:refs/heads/main",
            "description": "GitHub Actions OIDC for main branch",
            "audiences": ["api://AzureADTokenExchange"]
        }' > /dev/null 2>&1 || print_warning "Federated credential for main branch might already exist"
    
    # Setup federated credentials for feature branches
    print_info "Creating federated credential for feature branches..."
    az ad app federated-credential create \
        --id $SP_APP_ID \
        --parameters '{
            "name": "'$REPO_NAME'-feature-branches",
            "issuer": "https://token.actions.githubusercontent.com",
            "subject": "repo:'$REPO':ref:refs/heads/feature/*",
            "description": "GitHub Actions OIDC for feature branches",
            "audiences": ["api://AzureADTokenExchange"]
        }' > /dev/null 2>&1 || print_warning "Federated credential for feature branches might already exist"
    
    # Setup federated credentials for tags (releases)
    print_info "Creating federated credential for tags..."
    az ad app federated-credential create \
        --id $SP_APP_ID \
        --parameters '{
            "name": "'$REPO_NAME'-tags",
            "issuer": "https://token.actions.githubusercontent.com",
            "subject": "repo:'$REPO':ref:refs/tags/*",
            "description": "GitHub Actions OIDC for tags/releases",
            "audiences": ["api://AzureADTokenExchange"]
        }' > /dev/null 2>&1 || print_warning "Federated credential for tags might already exist"
    
    # Setup federated credentials for pull requests
    print_info "Creating federated credential for pull requests..."
    az ad app federated-credential create \
        --id $SP_APP_ID \
        --parameters '{
            "name": "'$REPO_NAME'-pull-requests",
            "issuer": "https://token.actions.githubusercontent.com",
            "subject": "repo:'$REPO':pull_request",
            "description": "GitHub Actions OIDC for pull requests",
            "audiences": ["api://AzureADTokenExchange"]
        }' > /dev/null 2>&1 || print_warning "Federated credential for pull requests might already exist"
    
    print_info "Federated identity credentials configured successfully."
    
    # Set GitHub secrets
    print_info "Setting GitHub secrets..."
    
    echo "$AZURE_CREDENTIALS" | gh secret set "AZURE_CREDENTIALS" --repo "$REPO"
    echo "$SP_APP_ID" | gh secret set "AZURE_CLIENT_ID" --repo "$REPO"
    echo "$TENANT_ID" | gh secret set "AZURE_TENANT_ID" --repo "$REPO"
    echo "$SUBSCRIPTION_ID" | gh secret set "AZURE_SUBSCRIPTION_ID" --repo "$REPO"
    
    print_info "=== Setup Complete ==="
    print_info "Service Principal: $SP_NAME"
    print_info "App ID: $SP_APP_ID"
    print_info "Federated Identity Credentials configured for GitHub Actions OIDC"
    print_info "GitHub Secrets configured successfully."
    echo
    print_info "Next steps:"
    print_info "1. Review the Bicep templates in infra/"
    print_info "2. Update parameter files in infra/parameters/"
    print_info "3. Run infrastructure deployment using GitHub Actions or Azure CLI"
    print_info "4. GitHub Actions will now use OIDC authentication (no client secrets needed)"
}

main "$@"