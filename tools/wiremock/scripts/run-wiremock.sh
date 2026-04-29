#!/usr/bin/env bash
# run-wiremock.sh — start a local WireMock standalone server backed by tools/wiremock/mappings/.
#
# Usage:
#   ./tools/wiremock/scripts/run-wiremock.sh
#   WIREMOCK_PORT=9091 ./tools/wiremock/scripts/run-wiremock.sh
#
# Endpoints:
#   - Admin UI:     http://localhost:${WIREMOCK_PORT}/__admin
#   - Search stub:  http://localhost:${WIREMOCK_PORT}/services/api/BooksTotal/Search/20170404
#
# Stops with Ctrl-C; the container is removed on exit (--rm).
set -euo pipefail

PORT="${WIREMOCK_PORT:-9090}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
IMAGE="wiremock/wiremock:3.13.1"

echo "Starting WireMock on http://localhost:${PORT} (mappings: ${ROOT}/mappings)"
exec docker run --rm \
  -p "${PORT}:8080" \
  -v "${ROOT}/mappings:/home/wiremock/mappings:ro" \
  "${IMAGE}" \
  --verbose \
  --global-response-templating
