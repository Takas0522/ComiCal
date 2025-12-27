<#
.SYNOPSIS
    Cosmos DB コンテナ初期化スクリプト
.DESCRIPTION
    ComiCal アプリケーション用の Cosmos DB データベースとコンテナを作成します。
    サーバーレスモードの Cosmos DB アカウントに最適化されています。
.PARAMETER CosmosAccountName
    Cosmos DB アカウント名
.PARAMETER ResourceGroupName
    Azure リソースグループ名
.PARAMETER DatabaseName
    作成するデータベース名（デフォルト: ComiCalDB）
.EXAMPLE
    .\setup-cosmosdb.ps1 -CosmosAccountName "mycosmosaccount" -ResourceGroupName "myresourcegroup"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$CosmosAccountName,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "ComiCalDB"
)

# エラー時に停止
$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cosmos DB コンテナ初期化スクリプト" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Azure CLI の存在確認
Write-Host "Azure CLI のバージョンを確認中..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✓ Azure CLI バージョン: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Error "Azure CLI がインストールされていません。https://docs.microsoft.com/cli/azure/install-azure-cli からインストールしてください。"
    exit 1
}

# Azure ログイン確認
Write-Host ""
Write-Host "Azure アカウントの認証状態を確認中..." -ForegroundColor Yellow
try {
    $account = az account show --output json 2>$null | ConvertFrom-Json
    if ($null -eq $account) {
        throw "Not logged in"
    }
    Write-Host "✓ ログイン済み: $($account.user.name)" -ForegroundColor Green
    Write-Host "  サブスクリプション: $($account.name)" -ForegroundColor Gray
} catch {
    Write-Host "Azure にログインしていません。ログインを開始します..." -ForegroundColor Yellow
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Azure ログインに失敗しました。"
        exit 1
    }
}

# Cosmos DB アカウントの存在確認
Write-Host ""
Write-Host "Cosmos DB アカウントの存在を確認中..." -ForegroundColor Yellow
try {
    $cosmosAccount = az cosmosdb show `
        --name $CosmosAccountName `
        --resource-group $ResourceGroupName `
        --output json 2>$null | ConvertFrom-Json
    
    if ($null -eq $cosmosAccount) {
        throw "Account not found"
    }
    
    Write-Host "✓ Cosmos DB アカウントが見つかりました: $CosmosAccountName" -ForegroundColor Green
    Write-Host "  リソースグループ: $ResourceGroupName" -ForegroundColor Gray
    Write-Host "  アカウントタイプ: $($cosmosAccount.kind)" -ForegroundColor Gray
} catch {
    Write-Error "Cosmos DB アカウント '$CosmosAccountName' が見つかりません。リソースグループ '$ResourceGroupName' を確認してください。"
    exit 1
}

# データベースの作成または確認
Write-Host ""
Write-Host "データベースを確認中..." -ForegroundColor Yellow
$dbExists = az cosmosdb sql database exists `
    --account-name $CosmosAccountName `
    --resource-group $ResourceGroupName `
    --name $DatabaseName `
    --output tsv 2>$null

if ($dbExists -eq "true") {
    Write-Host "✓ データベース '$DatabaseName' は既に存在します" -ForegroundColor Green
} else {
    Write-Host "データベース '$DatabaseName' を作成中..." -ForegroundColor Yellow
    az cosmosdb sql database create `
        --account-name $CosmosAccountName `
        --resource-group $ResourceGroupName `
        --name $DatabaseName `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ データベース '$DatabaseName' を作成しました" -ForegroundColor Green
    } else {
        Write-Error "データベースの作成に失敗しました"
        exit 1
    }
}

# comics コンテナの作成
Write-Host ""
Write-Host "comics コンテナを確認中..." -ForegroundColor Yellow
$comicsExists = az cosmosdb sql container exists `
    --account-name $CosmosAccountName `
    --resource-group $ResourceGroupName `
    --database-name $DatabaseName `
    --name "comics" `
    --output tsv 2>$null

if ($comicsExists -eq "true") {
    Write-Host "✓ comics コンテナは既に存在します" -ForegroundColor Green
} else {
    Write-Host "comics コンテナを作成中（インデックスポリシー設定）..." -ForegroundColor Yellow
    
    # インデックスポリシーの定義
    $indexPolicy = @{
        indexingMode = "consistent"
        automatic = $true
        includedPaths = @(
            @{
                path = "/*"
            }
        )
        excludedPaths = @(
            @{
                path = "/imageBaseUrl/?"
            },
            @{
                path = '/"_etag"/?'
            }
        )
    } | ConvertTo-Json -Depth 10 -Compress

    az cosmosdb sql container create `
        --account-name $CosmosAccountName `
        --resource-group $ResourceGroupName `
        --database-name $DatabaseName `
        --name "comics" `
        --partition-key-path "/id" `
        --idx "$indexPolicy" `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ comics コンテナを作成しました" -ForegroundColor Green
        Write-Host "  パーティションキー: /id" -ForegroundColor Gray
        Write-Host "  インデックスポリシー: /salesDate, /title, /author に範囲インデックス" -ForegroundColor Gray
        Write-Host "  除外パス: /imageBaseUrl" -ForegroundColor Gray
    } else {
        Write-Error "comics コンテナの作成に失敗しました"
        exit 1
    }
}

# config-migrations コンテナの作成
Write-Host ""
Write-Host "config-migrations コンテナを確認中..." -ForegroundColor Yellow
$configExists = az cosmosdb sql container exists `
    --account-name $CosmosAccountName `
    --resource-group $ResourceGroupName `
    --database-name $DatabaseName `
    --name "config-migrations" `
    --output tsv 2>$null

if ($configExists -eq "true") {
    Write-Host "✓ config-migrations コンテナは既に存在します" -ForegroundColor Green
} else {
    Write-Host "config-migrations コンテナを作成中..." -ForegroundColor Yellow
    
    az cosmosdb sql container create `
        --account-name $CosmosAccountName `
        --resource-group $ResourceGroupName `
        --database-name $DatabaseName `
        --name "config-migrations" `
        --partition-key-path "/id" `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ config-migrations コンテナを作成しました" -ForegroundColor Green
        Write-Host "  パーティションキー: /id" -ForegroundColor Gray
    } else {
        Write-Error "config-migrations コンテナの作成に失敗しました"
        exit 1
    }
}

# 接続文字列の取得と表示
Write-Host ""
Write-Host "接続文字列を取得中..." -ForegroundColor Yellow
$connectionString = az cosmosdb keys list `
    --name $CosmosAccountName `
    --resource-group $ResourceGroupName `
    --type connection-strings `
    --output json | ConvertFrom-Json

$primaryConnectionString = $connectionString.connectionStrings[0].connectionString

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "セットアップが完了しました！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "以下の接続文字列をアプリケーション設定に追加してください：" -ForegroundColor Yellow
Write-Host ""
Write-Host "local.settings.json または Azure Portal の設定:" -ForegroundColor Cyan
Write-Host '  "CosmosConnectionString": "' -NoNewline -ForegroundColor White
Write-Host $primaryConnectionString -NoNewline -ForegroundColor Yellow
Write-Host '"' -ForegroundColor White
Write-Host ""
Write-Host "作成されたリソース:" -ForegroundColor Cyan
Write-Host "  ✓ データベース: $DatabaseName" -ForegroundColor White
Write-Host "  ✓ コンテナ: comics (パーティションキー: /id)" -ForegroundColor White
Write-Host "  ✓ コンテナ: config-migrations (パーティションキー: /id)" -ForegroundColor White
Write-Host ""
