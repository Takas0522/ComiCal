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

# Function to get or create service principal
setup_service_principal() {
    local SP_NAME="$1"
    local SUBSCRIPTION_ID="$2"
    
    print_info "Setting up Service Principal: $SP_NAME"
    
    # Check if Service Principal already exists
    SP_APP_ID=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv)
    
    if [ -z "$SP_APP_ID" ]; then
        print_info "Creating new Service Principal..."
        
        # Create Service Principal with Contributor role
        SP_OUTPUT=$(az ad sp create-for-rbac \
            --name "$SP_NAME" \
            --role Contributor \
            --scopes /subscriptions/$SUBSCRIPTION_ID \
            --sdk-auth)
        
        SP_APP_ID=$(echo $SP_OUTPUT | jq -r '.clientId')
        print_info "Service Principal created successfully."
        print_info "  App ID: $SP_APP_ID"
    else
        print_warning "Service Principal already exists with App ID: $SP_APP_ID"
        print_info "Resetting Service Principal credentials..."
        
        # Reset credentials for existing Service Principal
        SP_CREDENTIALS=$(az ad sp credential reset \
            --id "$SP_APP_ID" \
            --query "{clientId: appId, clientSecret: password, tenantId: tenant, subscriptionId: '$SUBSCRIPTION_ID'}" \
            --output json)
        
        # Format for sdk-auth compatibility
        SP_OUTPUT=$(echo "$SP_CREDENTIALS" | jq '. + {
            "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
            "resourceManagerEndpointUrl": "https://management.azure.com/",
            "activeDirectoryGraphResourceId": "https://graph.windows.net/",
            "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
            "galleryEndpointUrl": "https://gallery.azure.com/",
            "managementEndpointUrl": "https://management.core.windows.net/"
        }')
    fi
    
    echo "$SP_OUTPUT"
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
    SP_CREDENTIALS=$(setup_service_principal "$SP_NAME" "$SUBSCRIPTION_ID")
    echo
    
    # Extract credentials
    CLIENT_ID=$(echo $SP_CREDENTIALS | jq -r '.clientId')
    CLIENT_SECRET=$(echo $SP_CREDENTIALS | jq -r '.clientSecret')
    
    # Set GitHub Secrets
    print_info "--- Configuring GitHub Secrets ---"
    
    # AZURE_CREDENTIALS (JSON format for backward compatibility)
    set_github_secret "$REPO" "AZURE_CREDENTIALS" "$SP_CREDENTIALS"
    
    # Individual secrets for OIDC/newer authentication
    set_github_secret "$REPO" "AZURE_CLIENT_ID" "$CLIENT_ID"
    set_github_secret "$REPO" "AZURE_TENANT_ID" "$TENANT_ID"
    set_github_secret "$REPO" "AZURE_SUBSCRIPTION_ID" "$SUBSCRIPTION_ID"
    
    echo
    print_info "=== Setup Complete ==="
    print_info "Service Principal: $SP_NAME"
    print_info "GitHub Secrets configured successfully."
    echo
    print_info "Next steps:"
    print_info "1. Review the Bicep templates in infra/"
    print_info "2. Update parameter files in infra/parameters/"
    print_info "3. Run infrastructure deployment using GitHub Actions or Azure CLI"
    echo
}

# Run main function
main "$@"
