#!/bin/bash

# Batch API Manual Testing Script
# このスクリプトは手動実行APIをテストします

set -e

# 設定
API_BASE_URL="${BATCH_API_URL:-http://localhost:7071}"
API_KEY="${BATCH_API_KEY:-dev-api-key-change-in-production}"

echo "=== Batch Manual Execution API テスト ==="
echo ""
echo "API Base URL: $API_BASE_URL"
echo "API Key: ${API_KEY:0:10}..."
echo ""

# 色付きログ用
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# テスト関数
test_endpoint() {
    local method=$1
    local path=$2
    local description=$3
    local expected_status=$4
    
    echo -e "${YELLOW}テスト: $description${NC}"
    echo "  $method $path"
    
    response=$(curl -s -w "\n%{http_code}" -X $method "$API_BASE_URL$path" \
        -H "X-API-Key: $API_KEY" \
        -H "Content-Type: application/json")
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" == "$expected_status" ]; then
        echo -e "  ${GREEN}✓ ステータスコード: $http_code${NC}"
        if [ -n "$body" ]; then
            echo "  レスポンス:"
            echo "$body" | jq '.' 2>/dev/null || echo "$body"
        fi
    else
        echo -e "  ${RED}✗ 期待: $expected_status, 実際: $http_code${NC}"
        if [ -n "$body" ]; then
            echo "  レスポンス:"
            echo "$body"
        fi
    fi
    echo ""
}

# 認証テスト
echo "=== 1. 認証テスト ==="
echo ""

echo -e "${YELLOW}テスト: 認証失敗（API Key なし）${NC}"
response=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/api/batch/registration")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" == "401" ]; then
    echo -e "  ${GREEN}✓ ステータスコード: $http_code（期待通り）${NC}"
else
    echo -e "  ${RED}✗ 期待: 401, 実際: $http_code${NC}"
fi
echo ""

echo -e "${YELLOW}テスト: 認証失敗（無効なAPI Key）${NC}"
response=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/api/batch/registration" \
    -H "X-API-Key: invalid-key")
http_code=$(echo "$response" | tail -n1)

if [ "$http_code" == "401" ]; then
    echo -e "  ${GREEN}✓ ステータスコード: $http_code（期待通り）${NC}"
else
    echo -e "  ${RED}✗ 期待: 401, 実際: $http_code${NC}"
fi
echo ""

# エンドポイントテスト
echo "=== 2. データ登録Job手動実行 ==="
echo ""
test_endpoint "POST" "/api/batch/registration" "データ登録Job実行" "200"

echo "=== 3. 画像ダウンロードJob手動実行 ==="
echo ""
test_endpoint "POST" "/api/batch/images" "画像ダウンロードJob実行" "200"

echo "=== 4. ページ範囲指定部分再実行 ==="
echo ""

# パラメータなしのテスト
echo -e "${YELLOW}テスト: パラメータ不足（startPage/endPage なし）${NC}"
response=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/api/batch/registration/partial" \
    -H "X-API-Key: $API_KEY")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" == "400" ]; then
    echo -e "  ${GREEN}✓ ステータスコード: $http_code（期待通り）${NC}"
    echo "  レスポンス:"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
else
    echo -e "  ${RED}✗ 期待: 400, 実際: $http_code${NC}"
fi
echo ""

# 正常なページ範囲
test_endpoint "POST" "/api/batch/registration/partial?startPage=1&endPage=5" \
    "部分再実行（ページ1-5）" "200"

# 無効なページ範囲
echo -e "${YELLOW}テスト: 無効なページ範囲（endPage < startPage）${NC}"
response=$(curl -s -w "\n%{http_code}" -X POST \
    "$API_BASE_URL/api/batch/registration/partial?startPage=10&endPage=5" \
    -H "X-API-Key: $API_KEY")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" == "400" ]; then
    echo -e "  ${GREEN}✓ ステータスコード: $http_code（期待通り）${NC}"
    echo "  レスポンス:"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
else
    echo -e "  ${RED}✗ 期待: 400, 実際: $http_code${NC}"
fi
echo ""

echo "=== 5. 手動介入解除 ==="
echo ""
test_endpoint "POST" "/api/batch/reset-intervention" \
    "手動介入解除（デフォルト：当日のバッチ）" "200"

# 特定のバッチIDでテスト
test_endpoint "POST" "/api/batch/reset-intervention?batchId=999999" \
    "手動介入解除（存在しないバッチID）" "404"

echo "=== テスト完了 ==="
echo ""
echo "注意事項:"
echo "- このスクリプトはAPIの接続とレスポンスのみをテストします"
echo "- 実際のJob実行は非同期でバックグラウンドで行われます"
echo "- Jobの実行状況はApplication InsightsやPostgreSQLで確認してください"
