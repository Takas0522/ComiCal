#!/usr/bin/env bash
# ComiCal DevContainer post-create
# - pnpm / Angular CLI / GitHub Copilot CLI / Playwright / Static Web Apps CLI / Azurite を導入
# - フロント・E2E が pnpm workspace 化された段階で `pnpm install` も実行する
set -euo pipefail

echo "==> corepack: enable pnpm"
# nvm 配下の node bin は vscode:nvm 所有で group-writable なため sudo は不要。
# むしろ sudo は secure_path で PATH をリセットしてしまい corepack が見つからない。
corepack enable
corepack prepare pnpm@latest --activate

echo "==> npm globals: Angular CLI / SWA CLI / Azurite / GitHub Copilot CLI / Playwright / Playwright CLI"
# GitHub Copilot CLI は npm パッケージ @github/copilot として提供される
# (旧 `gh extension install github/gh-copilot` は別物の旧 gh 拡張で非推奨)
# 参考: https://github.com/github/copilot-cli
# @playwright/test : E2E テストランナー (playwright コマンド)
#   参考: https://playwright.dev/docs/intro
# @playwright/cli  : コーディングエージェント向け軽量 CLI (playwright-cli コマンド)
#   参考: https://github.com/microsoft/playwright-cli
npm install -g \
  @angular/cli@latest \
  @azure/static-web-apps-cli@latest \
  azurite@latest \
  @github/copilot@latest \
  @playwright/test@latest \
  @playwright/cli@latest

echo "==> Playwright browsers + OS dependencies (Ubuntu 24.04 / noble)"
# ブラウザバイナリと apt 依存を一括取得 (root 権限が必要なため sudo を経由)
sudo --preserve-env=PATH "$(command -v playwright)" install --with-deps chromium firefox webkit || true

echo "==> playwright-cli skills"
# Claude Code / GitHub Copilot CLI 等が参照するスキルファイルをローカルに配置
playwright-cli install --skills || true

echo "==> Workspace bootstrap (idempotent)"
if [ -f "pnpm-workspace.yaml" ]; then
  pnpm install --frozen-lockfile || pnpm install
fi

if [ -f "src/backend/ComiCal.sln" ]; then
  dotnet restore src/backend/ComiCal.sln
fi

if [ -f "src/db/ComiCal.Database.sqlproj" ]; then
  dotnet build src/db/ComiCal.Database.sqlproj /p:NetCoreBuild=true || true
fi

echo "==> Done."
