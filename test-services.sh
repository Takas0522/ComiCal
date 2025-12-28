#!/bin/bash

echo "=== DevContainer サービス接続テスト ==="
echo ""

# Azurite (Azure Storage Emulator) のテスト
echo "1. Azurite (Azure Storage Emulator)"
echo "   - Blob Service (Port 10000):"
if curl -s -o /dev/null -w "%{http_code}" http://azurite:10000/devstoreaccount1?comp=list > /dev/null 2>&1; then
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://azurite:10000/devstoreaccount1?comp=list 2>&1)
    if [[ "$STATUS" == "401" ]] || [[ "$STATUS" == "400" ]]; then
        echo "     ✅ 接続成功 (認証が必要 - 正常)"
    else
        echo "     ⚠️  予期しないステータス: $STATUS"
    fi
else
    echo "     ❌ 接続失敗"
fi

echo "   - Queue Service (Port 10001):"
if curl -s -o /dev/null -w "%{http_code}" http://azurite:10001/ > /dev/null 2>&1; then
    echo "     ✅ 接続成功"
else
    echo "     ❌ 接続失敗"
fi

echo "   - Table Service (Port 10002):"
if curl -s -o /dev/null -w "%{http_code}" http://azurite:10002/ > /dev/null 2>&1; then
    echo "     ✅ 接続成功"
else
    echo "     ❌ 接続失敗"
fi

echo ""
echo "   接続文字列:"
echo "   DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;"
echo "   AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;"
echo "   BlobEndpoint=http://azurite:10000/devstoreaccount1;"
echo "   QueueEndpoint=http://azurite:10001/devstoreaccount1;"
echo "   TableEndpoint=http://azurite:10002/devstoreaccount1;"

echo ""
echo "2. Cosmos DB Emulator"
echo "   - HTTP Endpoint (Port 8081):"

# 複数のホスト名で試行
COSMOS_HOSTS=("comical-cosmosdb" "cosmosdb" "localhost" "172.19.0.2")
COSMOS_FOUND=false

for host in "${COSMOS_HOSTS[@]}"; do
    if timeout 2 curl -s -k "https://$host:8081/_explorer/index.html" > /dev/null 2>&1; then
        echo "     ✅ $host:8081 で接続成功"
        COSMOS_FOUND=true
        COSMOS_HOST=$host
        break
    elif timeout 2 curl -s "http://$host:8081/" > /dev/null 2>&1; then
        echo "     ✅ $host:8081 で接続成功"
        COSMOS_FOUND=true
        COSMOS_HOST=$host
        break
    fi
done

if [ "$COSMOS_FOUND" = false ]; then
    echo "     ⚠️  接続できません（コンテナが起動していない可能性があります）"
    echo ""
    echo "   注意: Cosmos DB Emulator は ARM64 システムでは起動に時間がかかる場合や、"
    echo "         リソース不足で起動しない場合があります。"
else
    echo ""
    echo "   接続文字列例:"
    echo "   AccountEndpoint=https://$COSMOS_HOST:8081/;"
    echo "   AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;"
fi

echo ""
echo "=== 推奨事項 ==="
echo ""
echo "Cosmos DB Emulatorが起動していない場合:"
echo "  1. VS Codeコマンドパレット（Ctrl+Shift+P）を開く"
echo "  2. 'Dev Containers: Rebuild Container' を実行"
echo "  3. コンテナの再構築完了後、このスクリプトを再実行"
echo ""
echo "または、ターミナルで以下を実行:"
echo "  docker-compose -f .devcontainer/docker-compose.yml up -d"
echo ""
echo "=== テスト完了 ==="
