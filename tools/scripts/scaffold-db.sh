#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# tools/scripts/scaffold-db.sh
#
# Scaffold EF Core entities + DbContext from the live SQL schema produced by
# publishing src/db (DACPAC) to a local Azurite/SQL Server instance.
# This is the Stage F entry point; Stage D leaves it unused but ready.
#
# Usage:
#   SQL_CONNECTION_STRING="Server=localhost,1433;Database=ComiCal;User Id=sa;Password=...;TrustServerCertificate=true" \
#     tools/scripts/scaffold-db.sh
#
# Prerequisites:
#   * dotnet-ef tool restored (dotnet tool restore)
#   * src/db DACPAC already published to the target database
# -----------------------------------------------------------------------------
set -euo pipefail

: "${SQL_CONNECTION_STRING:?SQL_CONNECTION_STRING must be set}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="${REPO_ROOT}/src/backend/infrastructure/ComiCal.Infrastructure.csproj"
STARTUP="${REPO_ROOT}/src/backend/api/ComiCal.Api.csproj"
OUT_DIR="Persistence/Generated"
CONTEXT_NAME="ComiCalScaffoldedDbContext"

echo "Scaffolding EF Core entities into ${PROJECT} (${OUT_DIR})..."

dotnet ef dbcontext scaffold \
    "${SQL_CONNECTION_STRING}" \
    Microsoft.EntityFrameworkCore.SqlServer \
    --project "${PROJECT}" \
    --startup-project "${STARTUP}" \
    --output-dir "${OUT_DIR}" \
    --context "${CONTEXT_NAME}" \
    --context-dir "${OUT_DIR}" \
    --use-database-names \
    --no-onconfiguring \
    --force

echo "Scaffold complete."
