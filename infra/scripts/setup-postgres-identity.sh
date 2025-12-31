#!/bin/bash

# ComiCal PostgreSQL Managed Identity Setup Script
# This script registers a Managed Identity as a PostgreSQL database user
# and grants necessary permissions for the Functions app

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

# Function to display usage
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Setup Managed Identity as PostgreSQL database user with necessary permissions.

OPTIONS:
    -e, --environment       Environment (dev or prod) [REQUIRED]
    -s, --server-name       PostgreSQL server name (optional, will be computed from environment)
    -d, --database          Database name (default: comical)
    -i, --identity-name     Managed Identity name (optional, will be computed from environment)
    -p, --project-name      Project name (default: comical)
    -l, --location          Azure location (default: japaneast)
    -h, --help              Display this help message

EXAMPLES:
    # Setup for dev environment
    $0 --environment dev

    # Setup for prod environment with custom database name
    $0 --environment prod --database mydb

    # Setup with explicit server and identity names
    $0 -e dev -s psql-comical-d-jpe -i func-comical-api-d-jpe

PREREQUISITES:
    - Azure CLI installed and logged in
    - PostgreSQL client (psql) installed
    - Administrator credentials for PostgreSQL server
    - Managed Identity already created in Azure

EOF
    exit 1
}

# Default values
ENVIRONMENT=""
SERVER_NAME=""
DATABASE_NAME="comical"
IDENTITY_NAME=""
PROJECT_NAME="comical"
LOCATION="japaneast"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -s|--server-name)
            SERVER_NAME="$2"
            shift 2
            ;;
        -d|--database)
            DATABASE_NAME="$2"
            shift 2
            ;;
        -i|--identity-name)
            IDENTITY_NAME="$2"
            shift 2
            ;;
        -p|--project-name)
            PROJECT_NAME="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -h|--help)
            usage
            ;;
        *)
            print_error "Unknown option: $1"
            usage
            ;;
    esac
done

# Validate required parameters
if [ -z "$ENVIRONMENT" ]; then
    print_error "Environment is required. Use -e or --environment option."
    usage
fi

if [[ ! "$ENVIRONMENT" =~ ^(dev|prod)$ ]]; then
    print_error "Environment must be 'dev' or 'prod'"
    exit 1
fi

# Location abbreviations
declare -A LOCATION_ABBR=(
    ["japaneast"]="jpe"
    ["japanwest"]="jpw"
    ["eastus"]="eus"
    ["westus"]="wus"
    ["eastasia"]="ea"
    ["southeastasia"]="sea"
)

LOCATION_SHORT="${LOCATION_ABBR[$LOCATION]}"
if [ -z "$LOCATION_SHORT" ]; then
    print_error "Unsupported location: $LOCATION"
    exit 1
fi

# Environment abbreviations
if [ "$ENVIRONMENT" == "dev" ]; then
    ENV_SHORT="d"
else
    ENV_SHORT="p"
fi

# Compute resource names if not provided
if [ -z "$SERVER_NAME" ]; then
    SERVER_NAME="psql-${PROJECT_NAME}-${ENV_SHORT}-${LOCATION_SHORT}"
fi

if [ -z "$IDENTITY_NAME" ]; then
    # Try to find the Functions app managed identity
    IDENTITY_NAME="func-${PROJECT_NAME}-api-${ENV_SHORT}-${LOCATION_SHORT}"
fi

# Resource group name
RESOURCE_GROUP="rg-${PROJECT_NAME}-${ENV_SHORT}-${LOCATION_SHORT}"

print_info "=== ComiCal PostgreSQL Managed Identity Setup ==="
print_info "Environment: $ENVIRONMENT"
print_info "Resource Group: $RESOURCE_GROUP"
print_info "PostgreSQL Server: $SERVER_NAME"
print_info "Database: $DATABASE_NAME"
print_info "Managed Identity: $IDENTITY_NAME"
echo

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if psql is installed
if ! command -v psql &> /dev/null; then
    print_error "PostgreSQL client (psql) is not installed."
    print_info "Install it with:"
    print_info "  Ubuntu/Debian: sudo apt-get install postgresql-client"
    print_info "  macOS: brew install postgresql"
    print_info "  Windows: Download from https://www.postgresql.org/download/windows/"
    exit 1
fi

# Check Azure login
print_step "Checking Azure login status..."
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi
print_info "✓ Azure login verified"

# Get PostgreSQL server details
print_step "Retrieving PostgreSQL server details..."
SERVER_FQDN=$(az postgres flexible-server show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$SERVER_NAME" \
    --query "fullyQualifiedDomainName" \
    --output tsv 2>/dev/null)

if [ -z "$SERVER_FQDN" ]; then
    print_error "PostgreSQL server '$SERVER_NAME' not found in resource group '$RESOURCE_GROUP'"
    exit 1
fi
print_info "✓ Server FQDN: $SERVER_FQDN"

# Get Managed Identity details
print_step "Retrieving Managed Identity details..."
IDENTITY_PRINCIPAL_ID=$(az identity show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$IDENTITY_NAME" \
    --query "principalId" \
    --output tsv 2>/dev/null)

if [ -z "$IDENTITY_PRINCIPAL_ID" ]; then
    print_error "Managed Identity '$IDENTITY_NAME' not found in resource group '$RESOURCE_GROUP'"
    print_info "Available identities:"
    az identity list --resource-group "$RESOURCE_GROUP" --query "[].name" --output tsv
    exit 1
fi

IDENTITY_CLIENT_ID=$(az identity show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$IDENTITY_NAME" \
    --query "clientId" \
    --output tsv)

print_info "✓ Managed Identity Principal ID: $IDENTITY_PRINCIPAL_ID"
print_info "✓ Managed Identity Client ID: $IDENTITY_CLIENT_ID"

# Prompt for administrator password
print_step "Configuring database access..."
read -sp "Enter PostgreSQL administrator password: " ADMIN_PASSWORD
echo

if [ -z "$ADMIN_PASSWORD" ]; then
    print_error "Administrator password is required"
    exit 1
fi

# Get administrator username
ADMIN_USERNAME=$(az postgres flexible-server show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$SERVER_NAME" \
    --query "administratorLogin" \
    --output tsv)

print_info "✓ Administrator username: $ADMIN_USERNAME"

# Create SQL script for Managed Identity setup
SQL_SCRIPT=$(cat << EOF
-- Create Azure AD extension if not exists
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'azure_ad') THEN
        CREATE EXTENSION azure_ad;
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Failed to create azure_ad extension: %', SQLERRM;
        RAISE EXCEPTION 'Azure AD extension is required but could not be created. Please ensure it is available on your PostgreSQL Flexible Server.';
END
\$\$;

-- Set Azure AD authentication
SET aad_validate_oids_in_tenant = off;

-- Verify azure_ad_user role exists
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'azure_ad_user') THEN
        RAISE EXCEPTION 'The azure_ad_user role does not exist. This role should be created automatically by the azure_ad extension.';
    END IF;
END
\$\$;

-- Create database role for Managed Identity
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '${IDENTITY_NAME}') THEN
        CREATE ROLE "${IDENTITY_NAME}" WITH LOGIN PASSWORD NULL IN ROLE azure_ad_user;
    ELSE
        RAISE NOTICE 'Role ${IDENTITY_NAME} already exists, skipping creation.';
    END IF;
END
\$\$;

-- Grant necessary permissions
GRANT CONNECT ON DATABASE ${DATABASE_NAME} TO "${IDENTITY_NAME}";
GRANT USAGE ON SCHEMA public TO "${IDENTITY_NAME}";
GRANT CREATE ON SCHEMA public TO "${IDENTITY_NAME}";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "${IDENTITY_NAME}";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "${IDENTITY_NAME}";

-- Grant default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "${IDENTITY_NAME}";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO "${IDENTITY_NAME}";

-- Display confirmation
SELECT 'Managed Identity ${IDENTITY_NAME} configured successfully' AS status;
EOF
)

# Execute SQL script
print_step "Executing SQL script to configure Managed Identity..."
export PGPASSWORD="$ADMIN_PASSWORD"

echo "$SQL_SCRIPT" | psql \
    --host="$SERVER_FQDN" \
    --port=5432 \
    --username="$ADMIN_USERNAME" \
    --dbname="$DATABASE_NAME" \
    --set=sslmode=require

if [ $? -eq 0 ]; then
    print_info "✓ Managed Identity configured successfully in database"
else
    print_error "Failed to configure Managed Identity in database"
    exit 1
fi

# Cleanup password from environment
unset PGPASSWORD

echo
print_info "=== Setup Complete ==="
print_info "Managed Identity '$IDENTITY_NAME' has been registered as a PostgreSQL user"
print_info "Connection string for Functions (Managed Identity):"
echo "Host=${SERVER_FQDN};Database=${DATABASE_NAME};Username=${IDENTITY_NAME}"
echo
print_info "Next steps:"
print_info "1. Update Functions app settings with the connection string"
print_info "2. Ensure Managed Identity is assigned to the Functions app"
print_info "3. Test the connection from Functions app"
echo
