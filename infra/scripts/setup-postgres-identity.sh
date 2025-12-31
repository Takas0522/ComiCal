#!/bin/bash

# ComiCal PostgreSQL Managed Identity Setup Script
# This script configures Managed Identity authentication for Azure Functions to access PostgreSQL
# It creates database users for Managed Identities and grants necessary permissions

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
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

print_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Function to check if required tools are installed
check_prerequisites() {
    print_info "Checking prerequisites..."
    
    if ! command -v az &> /dev/null; then
        print_error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
        exit 1
    fi
    
    if ! command -v psql &> /dev/null; then
        print_error "PostgreSQL client (psql) is not installed. Please install it."
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
    
    print_info "Logged in to Azure:"
    print_info "  Subscription: $SUBSCRIPTION_NAME"
    print_info "  Subscription ID: $SUBSCRIPTION_ID"
}

# Function to get PostgreSQL server details
get_postgres_details() {
    local ENV=$1
    local RESOURCE_GROUP=$2
    
    print_step "Retrieving PostgreSQL server details..."
    
    # Find PostgreSQL server in the resource group
    POSTGRES_SERVER=$(az postgres flexible-server list \
        --resource-group "$RESOURCE_GROUP" \
        --query "[0].name" -o tsv)
    
    if [ -z "$POSTGRES_SERVER" ]; then
        print_error "No PostgreSQL server found in resource group: $RESOURCE_GROUP"
        exit 1
    fi
    
    POSTGRES_FQDN=$(az postgres flexible-server show \
        --resource-group "$RESOURCE_GROUP" \
        --name "$POSTGRES_SERVER" \
        --query "fullyQualifiedDomainName" -o tsv)
    
    print_info "PostgreSQL Server: $POSTGRES_SERVER"
    print_info "PostgreSQL FQDN: $POSTGRES_FQDN"
}

# Function to configure Azure AD authentication
setup_aad_admin() {
    local RESOURCE_GROUP=$1
    local POSTGRES_SERVER=$2
    local ADMIN_USER=$3
    local ADMIN_OBJECT_ID=$4
    
    print_step "Configuring Azure AD administrator..."
    
    # Check if AD admin already exists
    EXISTING_ADMIN=$(az postgres flexible-server ad-admin list \
        --resource-group "$RESOURCE_GROUP" \
        --server-name "$POSTGRES_SERVER" \
        --query "[0].principalName" -o tsv 2>/dev/null || echo "")
    
    if [ -n "$EXISTING_ADMIN" ]; then
        print_warning "Azure AD admin already configured: $EXISTING_ADMIN"
        return
    fi
    
    # Create AD admin if object ID is provided
    if [ -n "$ADMIN_OBJECT_ID" ] && [ -n "$ADMIN_USER" ]; then
        print_info "Setting up Azure AD admin: $ADMIN_USER"
        az postgres flexible-server ad-admin create \
            --resource-group "$RESOURCE_GROUP" \
            --server-name "$POSTGRES_SERVER" \
            --object-id "$ADMIN_OBJECT_ID" \
            --display-name "$ADMIN_USER" \
            --no-wait
        
        print_info "Azure AD admin configuration initiated."
    else
        print_warning "Azure AD admin object ID or username not provided. Skipping AD admin setup."
    fi
}

# Function to get Managed Identity details for Functions
get_function_identity() {
    local RESOURCE_GROUP=$1
    local FUNCTION_APP_NAME=$2
    
    print_step "Retrieving Managed Identity for Function App: $FUNCTION_APP_NAME"
    
    # Check if function app exists
    if ! az functionapp show --resource-group "$RESOURCE_GROUP" --name "$FUNCTION_APP_NAME" &> /dev/null; then
        print_warning "Function App $FUNCTION_APP_NAME not found. Skipping identity setup."
        return 1
    fi
    
    # Enable system-assigned managed identity if not already enabled
    IDENTITY_PRINCIPAL_ID=$(az functionapp identity show \
        --resource-group "$RESOURCE_GROUP" \
        --name "$FUNCTION_APP_NAME" \
        --query principalId -o tsv 2>/dev/null || echo "")
    
    if [ -z "$IDENTITY_PRINCIPAL_ID" ]; then
        print_info "Enabling system-assigned managed identity..."
        IDENTITY_PRINCIPAL_ID=$(az functionapp identity assign \
            --resource-group "$RESOURCE_GROUP" \
            --name "$FUNCTION_APP_NAME" \
            --query principalId -o tsv)
    fi
    
    print_info "Managed Identity Principal ID: $IDENTITY_PRINCIPAL_ID"
    echo "$IDENTITY_PRINCIPAL_ID"
}

# Function to create database user for Managed Identity
create_database_user() {
    local POSTGRES_FQDN=$1
    local DATABASE=$2
    local ADMIN_USER=$3
    local ADMIN_PASSWORD=$4
    local IDENTITY_NAME=$5
    
    print_step "Creating database user for Managed Identity: $IDENTITY_NAME"
    
    # Check if user already exists
    local USER_EXISTS=$(PGPASSWORD="$ADMIN_PASSWORD" psql \
        --host="$POSTGRES_FQDN" \
        --port=5432 \
        --username="$ADMIN_USER" \
        --dbname=postgres \
        --set=sslmode=require \
        --tuples-only \
        --no-align \
        --command="SELECT 1 FROM pg_roles WHERE rolname='${IDENTITY_NAME}';" 2>/dev/null || echo "")
    
    if [ "$USER_EXISTS" = "1" ]; then
        print_warning "User '${IDENTITY_NAME}' already exists. Updating permissions..."
    fi
    
    # Create SQL commands (idempotent - will not fail if user exists)
    local SQL_COMMANDS=$(cat <<EOF
-- Create user for Managed Identity if not exists
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${IDENTITY_NAME}') THEN
        CREATE USER "${IDENTITY_NAME}" WITH LOGIN;
    END IF;
END
\$\$;

GRANT CONNECT ON DATABASE ${DATABASE} TO "${IDENTITY_NAME}";

-- Grant necessary permissions
\c ${DATABASE}
GRANT USAGE ON SCHEMA public TO "${IDENTITY_NAME}";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "${IDENTITY_NAME}";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "${IDENTITY_NAME}";

-- Grant permissions for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "${IDENTITY_NAME}";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO "${IDENTITY_NAME}";
EOF
)
    
    # Execute SQL commands with better error handling
    print_info "Executing SQL commands to create/update database user..."
    if echo "$SQL_COMMANDS" | PGPASSWORD="$ADMIN_PASSWORD" psql \
        --host="$POSTGRES_FQDN" \
        --port=5432 \
        --username="$ADMIN_USER" \
        --dbname=postgres \
        --set=sslmode=require \
        --set ON_ERROR_STOP=on 2>&1 | tee /tmp/psql-output.log; then
        print_info "Database user created/updated successfully."
    else
        print_error "Failed to create/update database user. Check /tmp/psql-output.log for details."
        return 1
    fi
}

# Function to display connection information
display_connection_info() {
    local POSTGRES_FQDN=$1
    local DATABASE=$2
    local IDENTITY_NAME=$3
    
    print_info "=== Connection Information ==="
    print_info "Host: $POSTGRES_FQDN"
    print_info "Database: $DATABASE"
    print_info "User: $IDENTITY_NAME"
    print_info ""
    print_info "Connection string template for Managed Identity:"
    print_info "Host=${POSTGRES_FQDN};Database=${DATABASE};Username=${IDENTITY_NAME};SSL Mode=Require"
    print_info ""
    print_info "Note: When using Managed Identity, authentication is handled automatically."
    print_info "Configure the connection string in Function App settings."
}

# Main execution
main() {
    print_info "=== ComiCal PostgreSQL Managed Identity Setup ==="
    echo
    
    # Parse arguments
    if [ "$#" -lt 2 ]; then
        print_error "Usage: $0 <environment> <resource-group> [admin-password]"
        print_error "Example: $0 dev rg-comical-d-jpe MySecurePassword123!"
        exit 1
    fi
    
    ENV=$1
    RESOURCE_GROUP=$2
    ADMIN_PASSWORD=${3:-""}
    DATABASE="comical"
    
    # Check prerequisites
    check_prerequisites
    echo
    
    # Check Azure login
    check_azure_login
    echo
    
    # Get PostgreSQL details
    get_postgres_details "$ENV" "$RESOURCE_GROUP"
    echo
    
    # Prompt for admin password if not provided
    if [ -z "$ADMIN_PASSWORD" ]; then
        print_info "PostgreSQL admin password is required to create database users."
        print_warning "SECURITY NOTE: Password will not be visible while typing."
        print_warning "For production use, consider using environment variables:"
        print_warning "  export PGPASSWORD='your-password'"
        print_warning "  Or use Azure Key Vault for secure password retrieval."
        read -s -p "Enter PostgreSQL admin password: " ADMIN_PASSWORD
        echo
        
        if [ -z "$ADMIN_PASSWORD" ]; then
            print_error "Password is required."
            exit 1
        fi
    fi
    
    # Setup Azure AD admin (optional)
    print_info "To configure Azure AD admin, provide the Object ID and username."
    print_info "Press Enter to skip AD admin setup."
    read -p "Azure AD admin Object ID (optional): " AAD_OBJECT_ID
    if [ -n "$AAD_OBJECT_ID" ]; then
        read -p "Azure AD admin username: " AAD_USERNAME
        setup_aad_admin "$RESOURCE_GROUP" "$POSTGRES_SERVER" "$AAD_USERNAME" "$AAD_OBJECT_ID"
        echo
    fi
    
    # Find and configure Function Apps
    print_step "Searching for Function Apps in resource group..."
    FUNCTION_APPS=$(az functionapp list \
        --resource-group "$RESOURCE_GROUP" \
        --query "[].name" -o tsv)
    
    if [ -z "$FUNCTION_APPS" ]; then
        print_warning "No Function Apps found in resource group: $RESOURCE_GROUP"
        print_info "Function Apps can be configured later when they are deployed."
    else
        print_info "Found Function Apps:"
        echo "$FUNCTION_APPS"
        echo
        
        # Configure each Function App
        for FUNC_APP in $FUNCTION_APPS; do
            print_info "Configuring Function App: $FUNC_APP"
            
            IDENTITY_ID=$(get_function_identity "$RESOURCE_GROUP" "$FUNC_APP")
            
            if [ -n "$IDENTITY_ID" ]; then
                # Use Function App name as the database user name
                create_database_user \
                    "$POSTGRES_FQDN" \
                    "$DATABASE" \
                    "psqladmin" \
                    "$ADMIN_PASSWORD" \
                    "$FUNC_APP"
                
                display_connection_info "$POSTGRES_FQDN" "$DATABASE" "$FUNC_APP"
            fi
            echo
        done
    fi
    
    echo
    print_info "=== Setup Complete ==="
    print_info "Next steps:"
    print_info "1. Configure connection strings in Function App settings"
    print_info "2. Test database connectivity from Function Apps"
    print_info "3. Initialize database schema if not already done"
    echo
}

# Run main function
main "$@"
