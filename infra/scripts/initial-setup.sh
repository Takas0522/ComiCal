#!/bin/bash

# ComiCal Infrastructure Initial Setup Script
# This script creates Azure Service Principal and configures GitHub Secrets

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if required tools are installed
check_prerequisites() {
    print_info "Checking prerequisites..."
    
    if ! command -v az &> /dev/null; then
        print_error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
        exit 1
    fi
    
    if ! command -v gh &> /dev/null; then
        print_error "GitHub CLI is not installed. Please install it from https://cli.github.com/"
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        print_error "jq is not installed. Please install it (e.g., sudo apt-get install jq)"
        exit 1
    fi
    
    print_info "All prerequisites are satisfied."
}

# Function to check Azure login status
check_azure_login() {
    print_info "Checking Azure login status..."
    
    if ! az account show &> /dev/null; then
        print_error "Not logged in to Azure. Please run 'az login' first."
        exit 1
    fi
    
    SUBSCRIPTION_ID=$(az account show --query id -o tsv)
    SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
    TENANT_ID=$(az account show --query tenantId -o tsv)
    
    print_info "Logged in to Azure:"
    print_info "  Subscription: $SUBSCRIPTION_NAME"
    print_info "  Subscription ID: $SUBSCRIPTION_ID"
    print_info "  Tenant ID: $TENANT_ID"
}

# Function to check GitHub authentication
check_github_auth() {
    print_info "Checking GitHub authentication..."
    
    if ! gh auth status &> /dev/null; then
        print_error "Not authenticated to GitHub. Please run 'gh auth login' first."
        exit 1
    fi
    
    print_info "GitHub authentication verified."
}

# Function to get or create service principal with OIDC support
setup_service_principal() {
    local SP_NAME="$1"
    local SUBSCRIPTION_ID="$2"
    local REPO="$3"
    
    print_info "Setting up Service Principal: $SP_NAME"
    
    # Check if Service Principal already exists
    SP_APP_ID=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv)
    
    if [ -z "$SP_APP_ID" ]; then
        print_info "Creating new Service Principal..."
        
        # Create Service Principal with Contributor role
        SP_CREATION_OUTPUT=$(az ad sp create-for-rbac \
            --name "$SP_NAME" \
            --role Contributor \
            --scopes /subscriptions/$SUBSCRIPTION_ID \
            --json-auth)
        
        SP_APP_ID=$(echo $SP_CREATION_OUTPUT | jq -r '.clientId')
        print_info "Service Principal created successfully."
        print_info "  App ID: $SP_APP_ID"
        
        # Format for sdk-auth compatibility (legacy format)
        SP_OUTPUT=$(echo $SP_CREATION_OUTPUT | jq '. + {
            "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
            "resourceManagerEndpointUrl": "https://management.azure.com/",
            "activeDirectoryGraphResourceId": "https://graph.windows.net/",
            "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
            "galleryEndpointUrl": "https://gallery.azure.com/",
            "managementEndpointUrl": "https://management.core.windows.net/"
        }')
    else
        print_warning "Service Principal already exists with App ID: $SP_APP_ID"
        print_info "Using existing Service Principal for OIDC setup..."
        
        # Get tenant ID
        TENANT_ID=$(az account show --query tenantId -o tsv)
        
        # Create a basic SP_OUTPUT structure for legacy compatibility
        SP_OUTPUT=$(jq -n --arg clientId "$SP_APP_ID" \
                         --arg subscriptionId "$SUBSCRIPTION_ID" \
                         --arg tenantId "$TENANT_ID" \
                         '{
                             clientId: $clientId,
                             subscriptionId: $subscriptionId,
                             tenantId: $tenantId,
                             activeDirectoryEndpointUrl: "https://login.microsoftonline.com",
                             resourceManagerEndpointUrl: "https://management.azure.com/",
                             activeDirectoryGraphResourceId: "https://graph.windows.net/",
                             sqlManagementEndpointUrl: "https://management.core.windows.net:8443/",
                             galleryEndpointUrl: "https://gallery.azure.com/",
                             managementEndpointUrl: "https://management.core.windows.net/"
                         }')
    fi
    
    # Setup federated identity credentials for OIDC authentication
    setup_federated_credentials "$SP_APP_ID" "$REPO"
    
    echo "$SP_OUTPUT"
}

# Function to setup federated identity credentials for OIDC
setup_federated_credentials() {
    local SP_APP_ID="$1"
    local REPO="$2"
    
    print_info "Setting up federated identity credentials for OIDC authentication..."
    
    # Extract owner and repo name
    local REPO_OWNER=$(echo $REPO | cut -d'/' -f1)
    local REPO_NAME=$(echo $REPO | cut -d'/' -f2)
    
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
    
    # Setup federated credentials for environments
    print_info "Creating federated credential for dev environment..."
    az ad app federated-credential create \
        --id $SP_APP_ID \
        --parameters '{
            "name": "'$REPO_NAME'-env-dev",
            "issuer": "https://token.actions.githubusercontent.com",
            "subject": "repo:'$REPO':environment:dev",
            "description": "GitHub Actions OIDC for dev environment",
            "audiences": ["api://AzureADTokenExchange"]
        }' > /dev/null 2>&1 || print_warning "Federated credential for dev environment might already exist"
    
    # Setup federated credentials for prod environment
    print_info "Creating federated credential for prod environment..."
    az ad app federated-credential create \
        --id $SP_APP_ID \
        --parameters '{
            "name": "'$REPO_NAME'-env-prod",
            "issuer": "https://token.actions.githubusercontent.com",
            "subject": "repo:'$REPO':environment:prod",
            "description": "GitHub Actions OIDC for prod environment",
            "audiences": ["api://AzureADTokenExchange"]
        }' > /dev/null 2>&1 || print_warning "Federated credential for prod environment might already exist"
}

# Function to set GitHub secret
set_github_secret() {
    local REPO="$1"
    local SECRET_NAME="$2"
    local SECRET_VALUE="$3"
    
    print_info "Setting GitHub secret: $SECRET_NAME"
    
    echo "$SECRET_VALUE" | gh secret set "$SECRET_NAME" --repo "$REPO"
    
    if [ $? -eq 0 ]; then
        print_info "Secret $SECRET_NAME set successfully."
    else
        print_error "Failed to set secret $SECRET_NAME"
        exit 1
    fi
}

# Main execution
main() {
    print_info "=== ComiCal Infrastructure Initial Setup ==="
    echo
    
    # Check prerequisites
    check_prerequisites
    echo
    
    # Check Azure login
    check_azure_login
    echo
    
    # Check GitHub authentication
    check_github_auth
    echo
    
    # Get GitHub repository
    REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
    print_info "GitHub Repository: $REPO"
    echo
    
    # Get subscription details
    SUBSCRIPTION_ID=$(az account show --query id -o tsv)
    TENANT_ID=$(az account show --query tenantId -o tsv)
    
    # Service Principal name
    SP_NAME="sp-comical-github-actions"
    
    # Setup Service Principal
    print_info "--- Setting up Service Principal ---"
    SP_CREDENTIALS=$(setup_service_principal "$SP_NAME" "$SUBSCRIPTION_ID" "$REPO")
    echo
    
    # Extract credentials
    CLIENT_ID=$(echo $SP_CREDENTIALS | jq -r '.clientId')
    
    # Set GitHub Secrets
    print_info "--- Configuring GitHub Secrets ---"
    
    # AZURE_CREDENTIALS (JSON format for backward compatibility - if needed)
    # Note: This is kept for legacy workflows, but OIDC auth doesn't need client secret
    set_github_secret "$REPO" "AZURE_CREDENTIALS" "$SP_CREDENTIALS"
    
    # Individual secrets for OIDC authentication (primary method)
    set_github_secret "$REPO" "AZURE_CLIENT_ID" "$CLIENT_ID"
    set_github_secret "$REPO" "AZURE_TENANT_ID" "$TENANT_ID"
    set_github_secret "$REPO" "AZURE_SUBSCRIPTION_ID" "$SUBSCRIPTION_ID"
    
    echo
    print_info "=== Setup Complete ==="
    print_info "Service Principal: $SP_NAME"
    print_info "App ID: $CLIENT_ID"
    print_info "Federated Identity Credentials configured for GitHub Actions OIDC"
    print_info "GitHub Secrets configured successfully."
    echo
    print_info "Next steps:"
    print_info "1. Review the Bicep templates in infra/"
    print_info "2. Update parameter files in infra/parameters/"
    print_info "3. Run infrastructure deployment using GitHub Actions or Azure CLI"
    print_info "4. GitHub Actions will now use OIDC authentication (no client secrets needed)"
    echo
}

# Run main function
main "$@"
