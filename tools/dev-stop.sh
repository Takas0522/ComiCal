#!/usr/bin/env bash
# ComiCal ローカル開発停止スクリプト
# 使い方: bash tools/dev-stop.sh
set -euo pipefail

log() { echo -e "\033[1;34m[dev-stop]\033[0m $*"; }
ok()  { echo -e "\033[1;32m[dev-stop]\033[0m $*"; }

for pidfile in /tmp/comical-api.pid /tmp/comical-batch.pid /tmp/comical-fe.pid; do
  if [[ -f "$pidfile" ]]; then
    PID=$(cat "$pidfile")
    if kill -0 "$PID" 2>/dev/null; then
      log "プロセス $PID を停止しています ($pidfile)..."
      kill "$PID" && ok "停止しました"
    fi
    rm -f "$pidfile"
  fi
done

log "Docker コンテナを停止しています..."
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
docker compose down
ok "全サービスを停止しました"
