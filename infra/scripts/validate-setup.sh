#!/bin/bash

# Test script to validate Bicep infrastructure setup
# This script can be run locally without Azure credentials to verify basic setup

# Don't exit on errors - we want to collect all validation results
set +e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Track validation results
ERROR_COUNT=0
WARNING_COUNT=0

print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}\n"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
    ((ERROR_COUNT++))
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
    ((WARNING_COUNT++))
}

# Check if running from repository root
if [ ! -f "infra/main.bicep" ]; then
    echo -e "${RED}✗${NC} Please run this script from the repository root directory"
    exit 1
fi

print_header "Validating Infrastructure Setup"

# 1. Check directory structure
print_header "Checking Directory Structure"

dirs=(
    "infra"
    "infra/scripts"
    "infra/parameters"
    "infra/modules"
    "docs"
)

for dir in "${dirs[@]}"; do
    if [ -d "$dir" ]; then
        print_success "Directory exists: $dir"
    else
        print_error "Directory missing: $dir"
    fi
done

# 2. Check required files
print_header "Checking Required Files"

files=(
    "infra/main.bicep"
    "infra/parameters/dev.bicepparam"
    "infra/parameters/prod.bicepparam"
    "infra/scripts/initial-setup.sh"
    "infra/README.md"
    "docs/GITHUB_ACTIONS_SETUP.md"
    ".github/workflows/infra-deploy.yml"
)

for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        print_success "File exists: $file"
    else
        print_error "File missing: $file"
    fi
done

# 3. Check script permissions
print_header "Checking Script Permissions"

if [ -x "infra/scripts/initial-setup.sh" ]; then
    print_success "initial-setup.sh is executable"
else
    print_warning "initial-setup.sh is not executable (use: chmod +x infra/scripts/initial-setup.sh)"
fi

# 4. Validate Bicep syntax (if Azure CLI is available)
print_header "Validating Bicep Templates"

if command -v az &> /dev/null; then
    echo "Azure CLI found, validating Bicep templates..."
    
    if az bicep build --file infra/main.bicep --stdout > /dev/null 2>&1; then
        print_success "main.bicep: Valid syntax"
    else
        print_error "main.bicep: Syntax errors found"
    fi
    
    # Run linter
    echo ""
    echo "Running Bicep linter..."
    az bicep lint --file infra/main.bicep || true
else
    print_warning "Azure CLI not found, skipping Bicep validation"
    echo "Install Azure CLI to enable validation: https://docs.microsoft.com/cli/azure/install-azure-cli"
fi

# 5. Validate bash script syntax
print_header "Validating Bash Scripts"

if bash -n infra/scripts/initial-setup.sh; then
    print_success "initial-setup.sh: Valid syntax"
else
    print_error "initial-setup.sh: Syntax errors found"
fi

# 6. Check .gitignore
print_header "Checking .gitignore Configuration"

gitignore_patterns=(
    "*.credentials.json"
    "**/secrets/"
    "infra/**/*.json"
)

for pattern in "${gitignore_patterns[@]}"; do
    if grep -q "$pattern" .gitignore; then
        print_success ".gitignore contains: $pattern"
    else
        print_warning ".gitignore missing pattern: $pattern"
    fi
done

# 7. Verify naming conventions in templates
print_header "Verifying Naming Conventions"

if grep -q 'rg-${projectName}-${environmentName}-${locationShort}' infra/main.bicep; then
    print_success "Resource group naming follows Azure CAF conventions"
else
    print_warning "Resource group naming may not follow conventions"
fi

# 8. Check semantic versioning logic
print_header "Checking Semantic Versioning Logic"

if grep -q "isSemanticVersion" infra/main.bicep && \
   grep -q "startsWith(gitTag, 'v')" infra/main.bicep; then
    print_success "Semantic versioning logic implemented"
else
    print_warning "Semantic versioning logic not found"
fi

# 9. Verify parameter files
print_header "Verifying Parameter Files"

for env in dev prod; do
    param_file="infra/parameters/${env}.bicepparam"
    if grep -q "environmentName = '${env}'" "$param_file"; then
        print_success "${env}.bicepparam: Environment correctly set to '${env}'"
    else
        print_error "${env}.bicepparam: Environment not correctly set"
    fi
done

# Summary
print_header "Validation Summary"

if [ $ERROR_COUNT -eq 0 ] && [ $WARNING_COUNT -eq 0 ]; then
    echo -e "${GREEN}✅ All checks passed!${NC}"
elif [ $ERROR_COUNT -eq 0 ]; then
    echo -e "${YELLOW}⚠ Validation completed with ${WARNING_COUNT} warning(s)${NC}"
else
    echo -e "${RED}❌ Validation failed with ${ERROR_COUNT} error(s) and ${WARNING_COUNT} warning(s)${NC}"
    echo ""
    echo "Please fix the errors before proceeding."
    exit 1
fi

echo ""
echo "Next steps:"
echo "1. Run ./infra/scripts/initial-setup.sh to configure Azure and GitHub"
echo "2. Deploy infrastructure: az deployment sub create --location japaneast --template-file infra/main.bicep --parameters infra/parameters/dev.bicepparam"
echo "3. Use GitHub Actions workflow for automated deployments"
echo ""
echo "For detailed instructions, see: docs/GITHUB_ACTIONS_SETUP.md"
