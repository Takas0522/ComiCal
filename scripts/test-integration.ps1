<#
.SYNOPSIS
    ComiCal 統合テストスクリプト

.DESCRIPTION
    API層、Batch層、フロントエンドの統合テストを自動実行します。

.PARAMETER Environment
    テスト環境（Local, Dev, Staging, Prod）

.PARAMETER RunAllTests
    すべてのテストを実行

.PARAMETER TestApi
    API層のテストのみ実行

.PARAMETER TestBatch
    Batch層のテストのみ実行

.PARAMETER TestFrontend
    フロントエンドのテストのみ実行

.PARAMETER ConsistencyWaitSeconds
    Cosmos DB の整合性確認の待機時間（秒）。デフォルト: 2秒

.PARAMETER ResponseTimeThresholdMs
    API レスポンスタイムの閾値（ミリ秒）。デフォルト: 2000ms

.EXAMPLE
    .\test-integration.ps1 -Environment Local -RunAllTests
    ローカル環境ですべてのテストを実行

.EXAMPLE
    .\test-integration.ps1 -Environment Dev -TestApi
    開発環境でAPIテストのみ実行

.EXAMPLE
    .\test-integration.ps1 -Environment Local -TestApi -ResponseTimeThresholdMs 1000
    ローカル環境でAPIテストを実行（レスポンスタイム閾値1秒）
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Local", "Dev", "Staging", "Prod")]
    [string]$Environment,

    [Parameter()]
    [switch]$RunAllTests,

    [Parameter()]
    [switch]$TestApi,

    [Parameter()]
    [switch]$TestBatch,

    [Parameter()]
    [switch]$TestFrontend,

    [Parameter()]
    [int]$ConsistencyWaitSeconds = 2,

    [Parameter()]
    [int]$ResponseTimeThresholdMs = 2000
)

# 色付きログ出力関数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-TestHeader {
    param([string]$TestName)
    Write-Host ""
    Write-ColorOutput "========================================" "Cyan"
    Write-ColorOutput "  $TestName" "Cyan"
    Write-ColorOutput "========================================" "Cyan"
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ""
    )
    if ($Passed) {
        Write-ColorOutput "✓ $TestName : PASSED" "Green"
        if ($Message) {
            Write-ColorOutput "  └─ $Message" "Gray"
        }
    } else {
        Write-ColorOutput "✗ $TestName : FAILED" "Red"
        if ($Message) {
            Write-ColorOutput "  └─ $Message" "Yellow"
        }
    }
}

# 環境ごとのエンドポイント設定
$endpoints = @{
    Local = @{
        ApiUrl = "http://localhost:7071/api"
        FrontendUrl = "http://localhost:4200"
    }
    Dev = @{
        ApiUrl = "https://comical-api-dev.azurewebsites.net/api"
        FrontendUrl = "https://dev.manrem.devtakas.jp"
    }
    Staging = @{
        ApiUrl = "https://comical-api-staging.azurewebsites.net/api"
        FrontendUrl = "https://staging.manrem.devtakas.jp"
    }
    Prod = @{
        ApiUrl = "https://comical-api-prod.azurewebsites.net/api"
        FrontendUrl = "https://manrem.devtakas.jp"
    }
}

$apiUrl = $endpoints[$Environment].ApiUrl
$frontendUrl = $endpoints[$Environment].FrontendUrl

$testResults = @{
    Total = 0
    Passed = 0
    Failed = 0
    Skipped = 0
}

# テスト: GetComics API - 基本検索
function Test-GetComicsBasic {
    Write-TestHeader "Test: GetComics API - 基本検索"
    $testResults.Total++

    try {
        $fromDate = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
        $body = @{
            SearchList = @("ワンピース")
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$apiUrl/ComicData?fromdate=$fromDate" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 30

        if ($response -and $response.Count -ge 0) {
            $testResults.Passed++
            Write-TestResult "GetComics API - 基本検索" $true "取得件数: $($response.Count)"
            return $true
        } else {
            $testResults.Failed++
            Write-TestResult "GetComics API - 基本検索" $false "レスポンスが空です"
            return $false
        }
    }
    catch {
        $testResults.Failed++
        Write-TestResult "GetComics API - 基本検索" $false $_.Exception.Message
        return $false
    }
}

# テスト: GetComics API - 複数キーワード検索
function Test-GetComicsMultipleKeywords {
    Write-TestHeader "Test: GetComics API - 複数キーワード検索"
    $testResults.Total++

    try {
        $fromDate = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
        $body = @{
            SearchList = @("ワンピース", "ナルト", "ブリーチ")
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$apiUrl/ComicData?fromdate=$fromDate" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 30

        if ($response -and $response.Count -ge 0) {
            $testResults.Passed++
            Write-TestResult "GetComics API - 複数キーワード検索" $true "取得件数: $($response.Count)"
            return $true
        } else {
            $testResults.Failed++
            Write-TestResult "GetComics API - 複数キーワード検索" $false "レスポンスが空です"
            return $false
        }
    }
    catch {
        $testResults.Failed++
        Write-TestResult "GetComics API - 複数キーワード検索" $false $_.Exception.Message
        return $false
    }
}

# テスト: GetComics API - 空のキーワード
function Test-GetComicsEmptyKeywords {
    Write-TestHeader "Test: GetComics API - 空のキーワード"
    $testResults.Total++

    try {
        $fromDate = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
        $body = @{
            SearchList = @()
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$apiUrl/ComicData?fromdate=$fromDate" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 30

        if ($response.Count -eq 0) {
            $testResults.Passed++
            Write-TestResult "GetComics API - 空のキーワード" $true "空の結果を正しく返しました"
            return $true
        } else {
            $testResults.Failed++
            Write-TestResult "GetComics API - 空のキーワード" $false "空のキーワードで結果が返されました"
            return $false
        }
    }
    catch {
        $testResults.Failed++
        Write-TestResult "GetComics API - 空のキーワード" $false $_.Exception.Message
        return $false
    }
}

# テスト: ConfigMigration API - 保存と取得
function Test-ConfigMigration {
    Write-TestHeader "Test: ConfigMigration API - 保存と取得"
    $testResults.Total++

    try {
        # 設定を保存
        $keywords = @("test-keyword-1", "test-keyword-2", "test-keyword-3")
        $body = $keywords | ConvertTo-Json

        $postResponse = Invoke-RestMethod -Uri "$apiUrl/ConfigMigration" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 30

        $migrationId = $postResponse.Id

        if (-not $migrationId) {
            $testResults.Failed++
            Write-TestResult "ConfigMigration API - 保存" $false "Migration ID が返されませんでした"
            return $false
        }

        # 設定を取得
        # Wait for Cosmos DB eventual consistency
        Start-Sleep -Seconds $ConsistencyWaitSeconds

        $getResponse = Invoke-RestMethod -Uri "$apiUrl/ConfigMigration?id=$migrationId" `
            -Method GET `
            -TimeoutSec 30

        if ($getResponse.Data -and $getResponse.Data.Count -eq $keywords.Count) {
            $testResults.Passed++
            Write-TestResult "ConfigMigration API - 保存と取得" $true "Migration ID: $migrationId"
            return $true
        } else {
            $testResults.Failed++
            Write-TestResult "ConfigMigration API - 保存と取得" $false "取得したデータが一致しません"
            return $false
        }
    }
    catch {
        $testResults.Failed++
        Write-TestResult "ConfigMigration API - 保存と取得" $false $_.Exception.Message
        return $false
    }
}

# テスト: API レスポンスタイム
function Test-ApiResponseTime {
    Write-TestHeader "Test: API レスポンスタイム"
    $testResults.Total++

    try {
        $fromDate = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
        $body = @{
            SearchList = @("test")
        } | ConvertTo-Json

        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        $response = Invoke-RestMethod -Uri "$apiUrl/ComicData?fromdate=$fromDate" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 30

        $stopwatch.Stop()
        $responseTime = $stopwatch.ElapsedMilliseconds

        if ($responseTime -lt $ResponseTimeThresholdMs) {
            $testResults.Passed++
            Write-TestResult "API レスポンスタイム" $true "レスポンスタイム: ${responseTime}ms (閾値: ${ResponseTimeThresholdMs}ms)"
            return $true
        } else {
            $testResults.Failed++
            Write-TestResult "API レスポンスタイム" $false "レスポンスタイムが閾値を超えています: ${responseTime}ms (閾値: ${ResponseTimeThresholdMs}ms)"
            return $false
        }
    }
    catch {
        $testResults.Failed++
        Write-TestResult "API レスポンスタイム" $false $_.Exception.Message
        return $false
    }
}

# テスト: Batch 処理のログ確認（ローカルのみ）
function Test-BatchProcessing {
    Write-TestHeader "Test: Batch 処理"
    
    if ($Environment -ne "Local") {
        $testResults.Skipped++
        Write-ColorOutput "⊘ Batch 処理テスト: SKIPPED (ローカル環境のみ)" "Yellow"
        return $null
    }

    $testResults.Total++

    Write-ColorOutput "Batch 処理テストは手動で実行してください:" "Cyan"
    Write-ColorOutput "1. cd batch" "Gray"
    Write-ColorOutput "2. func start" "Gray"
    Write-ColorOutput "3. ログを確認して以下を確認:" "Gray"
    Write-ColorOutput "   - 楽天APIからのデータ取得成功" "Gray"
    Write-ColorOutput "   - Cosmos DB への登録成功" "Gray"
    Write-ColorOutput "   - Blob Storage への画像保存成功" "Gray"

    $testResults.Skipped++
    Write-ColorOutput "⊘ Batch 処理テスト: MANUAL" "Yellow"
    return $null
}

# テスト: フロントエンドの可用性
function Test-FrontendAvailability {
    Write-TestHeader "Test: フロントエンドの可用性"
    $testResults.Total++

    try {
        $response = Invoke-WebRequest -Uri $frontendUrl -Method GET -TimeoutSec 30

        if ($response.StatusCode -eq 200) {
            $testResults.Passed++
            Write-TestResult "フロントエンドの可用性" $true "Status Code: $($response.StatusCode)"
            return $true
        } else {
            $testResults.Failed++
            Write-TestResult "フロントエンドの可用性" $false "Status Code: $($response.StatusCode)"
            return $false
        }
    }
    catch {
        $testResults.Failed++
        Write-TestResult "フロントエンドの可用性" $false $_.Exception.Message
        return $false
    }
}

# メイン処理
Write-ColorOutput "========================================" "Magenta"
Write-ColorOutput "  ComiCal 統合テスト" "Magenta"
Write-ColorOutput "  環境: $Environment" "Magenta"
Write-ColorOutput "========================================" "Magenta"
Write-Host ""

$startTime = Get-Date

# テスト実行
if ($RunAllTests -or $TestApi) {
    Test-GetComicsBasic
    Test-GetComicsMultipleKeywords
    Test-GetComicsEmptyKeywords
    Test-ConfigMigration
    Test-ApiResponseTime
}

if ($RunAllTests -or $TestBatch) {
    Test-BatchProcessing
}

if ($RunAllTests -or $TestFrontend) {
    Test-FrontendAvailability
}

$endTime = Get-Date
$duration = $endTime - $startTime

# 結果サマリー
Write-Host ""
Write-ColorOutput "========================================" "Magenta"
Write-ColorOutput "  テスト結果サマリー" "Magenta"
Write-ColorOutput "========================================" "Magenta"
Write-ColorOutput "環境: $Environment" "White"
Write-ColorOutput "実行時間: $($duration.TotalSeconds.ToString('F2'))秒" "White"
Write-Host ""
Write-ColorOutput "総テスト数: $($testResults.Total)" "White"
Write-ColorOutput "成功: $($testResults.Passed)" "Green"
Write-ColorOutput "失敗: $($testResults.Failed)" "Red"
Write-ColorOutput "スキップ: $($testResults.Skipped)" "Yellow"
Write-Host ""

if ($testResults.Failed -eq 0 -and $testResults.Total -gt 0) {
    Write-ColorOutput "✓ すべてのテストが成功しました！" "Green"
    exit 0
} elseif ($testResults.Total -eq 0) {
    Write-ColorOutput "⚠ テストが実行されませんでした。パラメータを確認してください。" "Yellow"
    Write-ColorOutput "使用方法: .\test-integration.ps1 -Environment Local -RunAllTests" "Gray"
    exit 1
} else {
    Write-ColorOutput "✗ $($testResults.Failed)個のテストが失敗しました。" "Red"
    exit 1
}
