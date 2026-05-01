#!/usr/bin/env bash
# =============================================================================
# ComiCal ローカル開発起動スクリプト
# 使い方: bash tools/dev-start.sh [--no-frontend] [--skip-dacpac]
#
# 起動順序:
#   1. Docker コンテナ起動 (SQL Server 2022 + Azurite)
#   2. devcontainer を Docker ネットワークに追加（初回のみ）
#   3. コンテナ IP を取得 → local.settings.json を動的更新
#   4. SQL Server 起動待ち → DACPAC デプロイ
#   5. dotnet build (Functions)
#   6. Azure Functions API  (port 7071) バックグラウンド起動
#   7. Azure Functions Batch (port 7072) バックグラウンド起動
#   8. Angular dev server   (port 4200)  フォアグラウンド起動
#      ※ --no-frontend オプション指定時はスキップ
# =============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NO_FRONTEND=false
SKIP_DACPAC=false

for arg in "$@"; do
  [[ "$arg" == "--no-frontend" ]] && NO_FRONTEND=true
  [[ "$arg" == "--skip-dacpac" ]] && SKIP_DACPAC=true
done

log()  { echo -e "\033[1;34m[dev-start]\033[0m $*"; }
ok()   { echo -e "\033[1;32m[dev-start]\033[0m $*"; }
warn() { echo -e "\033[1;33m[dev-start]\033[0m $*"; }
err()  { echo -e "\033[1;31m[dev-start]\033[0m $*" >&2; }

# ---------------------------------------------------------------------------
# 1. Docker コンテナ起動
# ---------------------------------------------------------------------------
log "Docker コンテナを起動しています..."
cd "$REPO_ROOT"
docker compose up -d
ok "コンテナ起動コマンド完了"

# ---------------------------------------------------------------------------
# 2. devcontainer を Docker ネットワークに追加（Docker-outside-of-Docker 対応）
# ---------------------------------------------------------------------------
NETWORK="comical_default"
CONTAINER_ID="$(hostname)"
# コンテナ名（docker inspect の Name フィールド）で接続済みか判定
CONTAINER_NAME=$(docker inspect "$CONTAINER_ID" --format '{{.Name}}' 2>/dev/null | tr -d '/')
if ! docker network inspect "$NETWORK" \
     --format '{{range .Containers}}{{.Name}} {{end}}' 2>/dev/null \
     | grep -qE "(${CONTAINER_NAME}|${CONTAINER_ID})"; then
  log "devcontainer ($CONTAINER_NAME) を $NETWORK ネットワークに追加しています..."
  docker network connect "$NETWORK" "$CONTAINER_ID" 2>/dev/null || true
  ok "ネットワーク接続完了"
else
  ok "devcontainer は既に $NETWORK に接続済み"
fi

# ---------------------------------------------------------------------------
# 3. コンテナ IP を取得し local.settings.json を動的更新
# ---------------------------------------------------------------------------
# コンテナが起動するまで少し待つ
sleep 3
SQL_IP=$(docker inspect comical-sqlserver \
  --format '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' 2>/dev/null | head -1)
AZURITE_IP=$(docker inspect comical-azurite \
  --format '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' 2>/dev/null | head -1)

if [[ -z "$SQL_IP" || -z "$AZURITE_IP" ]]; then
  err "コンテナ IP を取得できませんでした。docker compose up が失敗している可能性があります"
  docker compose logs --tail=20
  exit 1
fi
log "SQL Server IP: $SQL_IP  /  Azurite IP: $AZURITE_IP"

SQL_CONN="Server=${SQL_IP},1433;Database=ComiCal;User Id=sa;Password=P@ssw0rdComiCal1;TrustServerCertificate=True;Encrypt=True;"
AZURITE_CONN="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://${AZURITE_IP}:10000/devstoreaccount1;QueueEndpoint=http://${AZURITE_IP}:10001/devstoreaccount1;TableEndpoint=http://${AZURITE_IP}:10002/devstoreaccount1;"

write_settings() {
  local file="$1"
  local rakuten_id="${2:-__YOUR_RAKUTEN_APP_ID__}"
  cat > "$file" <<EOF
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "${AZURITE_CONN}",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "${SQL_CONN}",
    "StorageAccountUri": "${AZURITE_CONN}",
    "BlobBaseUrl": "http://${AZURITE_IP}:10000/devstoreaccount1/thumbnails",
    "RakutenApplicationId": "${rakuten_id}"
  }
}
EOF
}

# 既存の RakutenApplicationId を保持
API_SETTINGS="$REPO_ROOT/src/backend/api/ComiCal.Api/local.settings.json"
BATCH_SETTINGS="$REPO_ROOT/src/backend/batch/ComiCal.Batch/local.settings.json"

EXISTING_RAKUTEN="__YOUR_RAKUTEN_APP_ID__"
if [[ -f "$BATCH_SETTINGS" ]]; then
  EXISTING_RAKUTEN=$(python3 -c "import json,sys; d=json.load(open('$BATCH_SETTINGS')); print(d.get('Values',{}).get('RakutenApplicationId','__YOUR_RAKUTEN_APP_ID__'))" 2>/dev/null || echo "__YOUR_RAKUTEN_APP_ID__")
fi

write_settings "$API_SETTINGS"
write_settings "$BATCH_SETTINGS" "$EXISTING_RAKUTEN"
ok "local.settings.json を更新しました (SQL: $SQL_IP, Azurite: $AZURITE_IP)"

# ---------------------------------------------------------------------------
# 4. SQL Server ヘルスチェック待ち (最大 90 秒)
# ---------------------------------------------------------------------------
log "SQL Server の起動を待っています (最大 90 秒)..."
for i in $(seq 1 18); do
  if docker exec comical-sqlserver \
       /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "P@ssw0rdComiCal1" \
       -Q "SELECT 1" -No >/dev/null 2>&1; then
    ok "SQL Server が起動しました"
    break
  fi
  if [[ $i -eq 18 ]]; then
    err "SQL Server が起動しませんでした (90 秒タイムアウト)"
    docker logs comical-sqlserver --tail 20
    exit 1
  fi
  echo -n "."
  sleep 5
done

# ---------------------------------------------------------------------------
# 5. DACPAC デプロイ
# ---------------------------------------------------------------------------
if $SKIP_DACPAC; then
  warn "DACPAC デプロイをスキップします (--skip-dacpac)"
else
  log "DACPAC をビルドしています..."
  dotnet build "$REPO_ROOT/src/db/ComiCal.Database.sqlproj" -c Debug --nologo -v quiet

  DACPAC="$REPO_ROOT/src/db/bin/Debug/ComiCal.Database.dacpac"
  log "DACPAC をデプロイしています → ComiCal データベース (${SQL_IP}:1433)..."

  # sqlpackage の場所を解決:
  #   1. PATH (DevContainer Dockerfile が /usr/local/bin/sqlpackage にシンボリックリンクを作成)
  #   2. /opt/sqlpackage/sqlpackage (Dockerfile での実体配置先)
  #   3. $HOME/.dotnet/tools/sqlpackage (dotnet global tool としてインストールしている場合のフォールバック)
  if command -v sqlpackage >/dev/null 2>&1; then
    SQLPACKAGE_BIN="$(command -v sqlpackage)"
  elif [[ -x /opt/sqlpackage/sqlpackage ]]; then
    SQLPACKAGE_BIN="/opt/sqlpackage/sqlpackage"
  elif [[ -x "$HOME/.dotnet/tools/sqlpackage" ]]; then
    SQLPACKAGE_BIN="$HOME/.dotnet/tools/sqlpackage"
  else
    err "sqlpackage が見つかりません。DevContainer を再ビルドしてください (.devcontainer/Dockerfile が /usr/local/bin/sqlpackage を作成します)"
    exit 1
  fi

  "$SQLPACKAGE_BIN" \
    /Action:Publish \
    /SourceFile:"$DACPAC" \
    /TargetConnectionString:"${SQL_CONN}" \
    /p:CreateNewDatabase=true \
    /p:BlockOnPossibleDataLoss=false \
    /p:ExcludeObjectTypes=FullTextCatalogs 2>&1 | tail -5
  ok "データベースのデプロイが完了しました"
fi

# ---------------------------------------------------------------------------
# 6. dotnet build (Functions)
# ---------------------------------------------------------------------------
log ".NET ソリューションをビルドしています..."
dotnet build "$REPO_ROOT/src/backend/ComiCal.slnx" -c Debug --nologo -v quiet
ok ".NET ビルド完了"

# ---------------------------------------------------------------------------
# 7. Functions API 起動 (port 7071) — バックグラウンド
# ---------------------------------------------------------------------------
# 既存プロセスを停止
if [[ -f /tmp/comical-api.pid ]]; then
  OLD_PID=$(cat /tmp/comical-api.pid)
  kill "$OLD_PID" 2>/dev/null || true
fi

API_DIR="$REPO_ROOT/src/backend/api/ComiCal.Api"
API_LOG="/tmp/comical-api.log"
log "Azure Functions API を起動しています (port 7071) → $API_LOG"
cd "$API_DIR"
setsid nohup func start --port 7071 > "$API_LOG" 2>&1 &
API_PID=$!
echo "$API_PID" > /tmp/comical-api.pid
log "Functions API PID: $API_PID"

# ---------------------------------------------------------------------------
# 8. Functions Batch 起動 (port 7072) — バックグラウンド
# ---------------------------------------------------------------------------
if [[ -f /tmp/comical-batch.pid ]]; then
  OLD_PID=$(cat /tmp/comical-batch.pid)
  kill "$OLD_PID" 2>/dev/null || true
fi

BATCH_DIR="$REPO_ROOT/src/backend/batch/ComiCal.Batch"
BATCH_LOG="/tmp/comical-batch.log"
log "Azure Functions Batch を起動しています (port 7072) → $BATCH_LOG"
cd "$BATCH_DIR"
setsid nohup func start --port 7072 > "$BATCH_LOG" 2>&1 &
BATCH_PID=$!
echo "$BATCH_PID" > /tmp/comical-batch.pid
log "Functions Batch PID: $BATCH_PID"

cd "$REPO_ROOT"

# Functions が起動するまで待つ (最大 60 秒)
log "Functions の起動を待っています..."
for i in $(seq 1 12); do
  if grep -q "Worker process started and initialized" "$API_LOG" 2>/dev/null \
     && grep -q "Worker process started and initialized" "$BATCH_LOG" 2>/dev/null; then
    ok "両 Functions が起動しました"
    break
  fi
  if [[ $i -eq 12 ]]; then
    warn "60 秒以内に起動確認できませんでした。ログを確認してください"
    warn "  API ログ  : $API_LOG"
    warn "  Batch ログ: $BATCH_LOG"
    break
  fi
  echo -n "."
  sleep 5
done
echo ""

# ---------------------------------------------------------------------------
# 起動後サマリ
# ---------------------------------------------------------------------------
if [[ "$EXISTING_RAKUTEN" == "__YOUR_RAKUTEN_APP_ID__" ]]; then
  warn "RakutenApplicationId が未設定です。バッチを実行するには:"
  warn "  $BATCH_SETTINGS の RakutenApplicationId を実際の楽天アプリIDに変更してください"
  echo ""
fi

# ---------------------------------------------------------------------------
# 9. Angular dev server 起動 — バックグラウンド (host 0.0.0.0 でホストからアクセス可能)
# ---------------------------------------------------------------------------
if [[ -f /tmp/comical-fe.pid ]]; then
  OLD_PID=$(cat /tmp/comical-fe.pid)
  kill "$OLD_PID" 2>/dev/null || true
fi

FE_LOG="/tmp/comical-fe.log"

if $NO_FRONTEND; then
  ok "--no-frontend 指定のため Angular は起動しません"
else
  log "Angular dev server を起動しています (port 4200, host 0.0.0.0) → $FE_LOG"
  cd "$REPO_ROOT"
  setsid nohup pnpm --filter frontend dev:local > "$FE_LOG" 2>&1 &
  FE_PID=$!
  echo "$FE_PID" > /tmp/comical-fe.pid
  log "Angular PID: $FE_PID"

  # Angular のコンパイル完了を待つ (最大 120 秒)
  log "Angular のビルドを待っています (初回は 60〜90 秒かかります)..."
  for i in $(seq 1 24); do
    if grep -q "Local:" "$FE_LOG" 2>/dev/null || grep -q "Application bundle generation complete" "$FE_LOG" 2>/dev/null; then
      ok "Angular dev server が起動しました"
      break
    fi
    if [[ $i -eq 24 ]]; then
      warn "120 秒以内に起動確認できませんでした (ログ: $FE_LOG)"
      break
    fi
    echo -n "."
    sleep 5
  done
  echo ""
fi

# ---------------------------------------------------------------------------
# 起動完了サマリ（更新）
# ---------------------------------------------------------------------------
echo ""
ok "===== 全サービスが起動しました ====="
echo ""
echo "  Angular        : http://localhost:4200  (ホストブラウザからアクセス可)"
echo "  Functions API  : http://localhost:7071"
echo "  Functions Batch: http://localhost:7072/api/batch/trigger"
echo "  SQL Server     : ${SQL_IP}:1433"
echo "  Azurite Blob   : http://${AZURITE_IP}:10000/devstoreaccount1"
echo ""
echo "  Angular ログ: tail -f $FE_LOG"
echo "  API ログ    : tail -f $API_LOG"
echo "  Batch ログ  : tail -f $BATCH_LOG"
echo ""
echo "停止するには: bash tools/dev-stop.sh"
