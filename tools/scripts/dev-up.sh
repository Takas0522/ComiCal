#!/usr/bin/env bash
# dev-up.sh — bring up a fully working ComiCal local dev environment.
#
# Components:
#   * Azurite           (Storage emulator, ports 10000-10002)
#   * WireMock          (Rakuten Books API stub on :8080, see tools/wiremock/)
#   * MSSQL Testcontainer | local docker mssql (informational; backend Integration tests
#                       spin up their own via Testcontainers)
#   * Functions API     (`func start --csharp --port 7071` from src/backend/api/)
#   * Functions Batch   (`func start --csharp --port 7072` from src/backend/batch/)
#   * SWA emulator      (`swa start` from src/frontend/, default port 4280)
#
# Usage:
#   ./tools/scripts/dev-up.sh
#
# Stop with Ctrl+C — the script traps SIGINT and stops all child processes.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LOG_DIR="${ROOT}/.dev-logs"
mkdir -p "${LOG_DIR}"

PIDS=()
trap 'echo "==> Stopping…"; for p in "${PIDS[@]}"; do kill "$p" 2>/dev/null || true; done; wait || true' INT TERM

start() {
  local name="$1"; shift
  echo "==> Starting ${name}: $*"
  ( "$@" ) > "${LOG_DIR}/${name}.log" 2>&1 &
  PIDS+=("$!")
}

# 1. Azurite (idempotent — fails fast if port in use; that's fine).
start azurite azurite --silent --location "${LOG_DIR}/azurite" --debug "${LOG_DIR}/azurite-debug.log"

# 2. WireMock for Rakuten Books API (mappings under tools/wiremock).
if [[ -d "${ROOT}/tools/wiremock" ]]; then
  start wiremock npx --yes wiremock --root-dir "${ROOT}/tools/wiremock" --port 8080 --no-request-journal
fi

# 3. Functions API (port 7071).
start func-api bash -lc "cd '${ROOT}/src/backend/api' && func start --csharp --port 7071"

# 4. Functions Batch (port 7072).
if [[ -d "${ROOT}/src/backend/batch" ]]; then
  start func-batch bash -lc "cd '${ROOT}/src/backend/batch' && func start --csharp --port 7072"
fi

# 5. Wait for /api/health on the Functions host before fronting it with SWA.
echo "==> Waiting for /api/health on :7071 …"
for i in {1..60}; do
  if curl -fsS "http://localhost:7071/api/v1/health" >/dev/null 2>&1; then
    echo "==> Functions API healthy."
    break
  fi
  sleep 1
done

# 6. SWA emulator — uses src/frontend/swa-cli.config.json (configuration name: comical).
start swa bash -lc "cd '${ROOT}/src/frontend' && swa start comical"

echo "==> All processes started. Logs in ${LOG_DIR}/. Press Ctrl+C to stop."
echo "    Frontend (SWA):  http://localhost:4280"
echo "    Frontend (ng):   http://localhost:4200"
echo "    Functions API:   http://localhost:7071"
echo "    Functions Batch: http://localhost:7072"
echo "    Auth (mock):     http://localhost:4280/.auth/login/aadb2c"
wait
