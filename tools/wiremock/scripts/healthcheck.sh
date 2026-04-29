#!/usr/bin/env bash
# healthcheck.sh — probes the WireMock admin endpoint and exits non-zero on failure.
#
# Usage:
#   ./tools/wiremock/scripts/healthcheck.sh
#   WIREMOCK_PORT=9091 ./tools/wiremock/scripts/healthcheck.sh
set -euo pipefail

PORT="${WIREMOCK_PORT:-9090}"
URL="http://localhost:${PORT}/__admin/health"

http_code="$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$URL" || echo "000")"
if [[ "$http_code" != "200" ]]; then
  echo "WireMock health check failed: ${URL} returned HTTP ${http_code}" >&2
  exit 1
fi
echo "WireMock OK (${URL})"
