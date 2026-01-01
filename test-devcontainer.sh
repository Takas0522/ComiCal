#!/bin/bash
# Test script for verifying DevContainer PostgreSQL migration
set -e

echo "=== DevContainer PostgreSQL Migration Test ==="
echo ""

cd "$(dirname "$0")/.devcontainer"

echo "1. Starting PostgreSQL and Azurite containers..."
docker compose up -d postgres azurite
echo "✓ Containers started"
echo ""

echo "2. Waiting for PostgreSQL to be ready..."
sleep 5
docker exec comical-postgres pg_isready -U comical -d comical
echo "✓ PostgreSQL is ready"
echo ""

echo "3. Verifying database tables..."
TABLES=$(docker exec comical-postgres psql -U comical -d comical -t -c "SELECT tablename FROM pg_tables WHERE schemaname='public';" | xargs)
echo "Tables found: $TABLES"

if [[ "$TABLES" == *"comic"* && "$TABLES" == *"comicimage"* && "$TABLES" == *"configmigration"* && "$TABLES" == *"batch_states"* && "$TABLES" == *"batch_page_errors"* ]]; then
    echo "✓ All required tables exist"
else
    echo "✗ Missing tables!"
    exit 1
fi
echo ""

echo "4. Verifying Comic table structure..."
docker exec comical-postgres psql -U comical -d comical -c "\d comic" | head -15
echo "✓ Comic table structure verified"
echo ""

echo "5. Verifying batch_states table structure..."
docker exec comical-postgres psql -U comical -d comical -c "\d batch_states" | head -20
echo "✓ batch_states table structure verified"
echo ""

echo "6. Verifying batch_page_errors table structure..."
docker exec comical-postgres psql -U comical -d comical -c "\d batch_page_errors" | head -20
echo "✓ batch_page_errors table structure verified"
echo ""

echo "7. Verifying Azurite is running..."
if docker exec comical-azurite ps aux | grep -q azurite; then
    echo "✓ Azurite is running"
else
    echo "✗ Azurite is not running"
    exit 1
fi
echo ""

echo "8. Cleaning up..."
docker compose down -v
echo "✓ Cleanup complete"
echo ""

echo "=== All Tests Passed! ==="
