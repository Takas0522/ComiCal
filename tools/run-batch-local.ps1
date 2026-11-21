<#
.SYNOPSIS
  ComiCal の日次収集バッチ（src/backend/batch/ComiCal.Batch, Durable Functions）をローカルで 1 回実行する。

.DESCRIPTION
  Azure 上の Batch Function App / NAT Gateway を廃止したため（docs/adr/0004-azure-cost-reduction.md）、
  バッチはこのスクリプトでメンテナーの PC から実行する。Windows タスク スケジューラからの起動を想定
  （tools/register-batch-task.ps1）。

  1. az のログインと楽天 API 許可 IP（任意）を確認
  2. Key Vault から楽天の認証情報を取得
  3. Azurite（Durable のタスクハブ用）を起動
  4. SQL Server ファイアウォールに現在のグローバル IP を一時追加
  5. Functions ホストを起動し、HTTP トリガーで DailyFetchOrchestrator を開始
  6. 完了まで待機 → ホスト停止・ファイアウォール規則削除
  失敗時は終了コード 1（タスク スケジューラの「前回の実行結果」に出る）と、任意で Webhook 通知。

  前提: Azure CLI (az login 済み), Azure Functions Core Tools v4, .NET 10 SDK,
        Docker Desktop もしくは npm の azurite, （任意）go-sqlcmd。
  権限: Storage Blob Data Contributor（Storage）, Key Vault Secrets User（Key Vault）,
        DB ユーザー（db_datareader/db_datawriter/EXECUTE）, SQL Server のファイアウォール規則を変更できるロール。
        Bicep の batchOperatorPrincipalId と cd-prod.yml の BATCH_OPERATOR_PRINCIPAL_ID で付与される。
#>
[CmdletBinding()]
param(
  [string]$Prefix = 'cmcl',
  [string]$Environment = 'prod',
  [int]$Port = 7072,
  [int]$TimeoutMinutes = 240,
  # 楽天アプリに登録した許可 IP。指定すると現在のグローバル IP と照合し、違えば実行前に中止する。
  [string]$ExpectedPublicIp = $env:COMICAL_RAKUTEN_ALLOWED_IP,
  # 失敗時（と -NotifyOnSuccess 時）に {"text": "..."} を POST する Webhook（Slack/Teams/Discord 互換）
  [string]$WebhookUrl = $env:COMICAL_BATCH_WEBHOOK_URL,
  [switch]$NotifyOnSuccess,
  # Application Insights（本番）へテレメトリを送る
  [switch]$EnableTelemetry
)

$ErrorActionPreference = 'Stop'
$repoRoot   = Split-Path -Parent $PSScriptRoot
$batchDir   = Join-Path $repoRoot 'src\backend\batch\ComiCal.Batch'
$logDir     = Join-Path $repoRoot '.dev-logs\batch'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp      = Get-Date -Format 'yyyyMMdd-HHmmss'
$logFile    = Join-Path $logDir "$stamp.log"
$hostLog    = Join-Path $logDir "$stamp.func.log"
$hostErrLog = Join-Path $logDir "$stamp.func.err.log"
$lockFile   = Join-Path $logDir '.lock'

$rg        = "$Prefix-$Environment-jpe-rg"
$sqlServer = "$Prefix-$Environment-jpe-sql"
$sqlDb     = "$Prefix-$Environment-jpe-sqldb"
$storage   = "$Prefix$($Environment)jpest"
$kvName    = "$Prefix-$Environment-jpe-kv"
$fwRule    = "local-batch-$($env:COMPUTERNAME)".ToLower()

Start-Transcript -Path $logFile -Append | Out-Null

function Write-Step($msg) { Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $msg) }

function Send-Notify($text) {
  if ([string]::IsNullOrWhiteSpace($WebhookUrl)) { return }
  try {
    $body = @{ text = $text; content = $text } | ConvertTo-Json -Compress
    Invoke-RestMethod -Method Post -Uri $WebhookUrl -ContentType 'application/json; charset=utf-8' `
      -Body ([Text.Encoding]::UTF8.GetBytes($body)) | Out-Null
  } catch { Write-Warning "Webhook 通知に失敗: $($_.Exception.Message)" }
}

function Test-Port([int]$p) {
  $c = New-Object Net.Sockets.TcpClient
  try { $iar = $c.BeginConnect('127.0.0.1', $p, $null, $null); return ($iar.AsyncWaitHandle.WaitOne(500) -and $c.Connected) }
  catch { return $false } finally { $c.Close() }
}

function Invoke-Az {
  # az の失敗を例外にする（PowerShell 5.1 ではネイティブコマンドの終了コードで例外にならないため）
  $out = & az @args 2>&1
  if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') failed: $out" }
  return $out
}

$funcProc = $null
$fwCreated = $false
$exitCode = 1
$summary = ''

try {
  if (Test-Path $lockFile) {
    $age = (Get-Date) - (Get-Item $lockFile).LastWriteTime
    if ($age.TotalMinutes -lt $TimeoutMinutes) { throw "別のバッチ実行中です（$lockFile）。" }
    Write-Warning "古いロックファイルを削除します（$([int]$age.TotalMinutes) 分前）。"
  }
  Set-Content -Path $lockFile -Value $PID

  # ── 1. 前提確認 ────────────────────────────────────────────────
  foreach ($cmd in 'az', 'func', 'dotnet') {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) { throw "$cmd が見つかりません。" }
  }
  Write-Step 'az ログイン確認'
  $null = Invoke-Az account show -o none

  $publicIp = (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).ToString().Trim()
  Write-Step "現在のグローバル IP: $publicIp"
  if (-not [string]::IsNullOrWhiteSpace($ExpectedPublicIp) -and $publicIp -ne $ExpectedPublicIp.Trim()) {
    throw "グローバル IP が楽天の許可 IP（$ExpectedPublicIp）と異なります。楽天ウェブサービスのアプリ設定を更新してから再実行してください。"
  }

  # ── 2. シークレット取得 ─────────────────────────────────────────
  Write-Step "Key Vault ($kvName) から楽天の認証情報を取得"
  $rakutenAppId     = (Invoke-Az keyvault secret show --vault-name $kvName -n RakutenAppId --query value -o tsv | Out-String).Trim()
  $rakutenAccessKey = (Invoke-Az keyvault secret show --vault-name $kvName -n RakutenAccessKey --query value -o tsv | Out-String).Trim()
  $rakutenAffId     = (Invoke-Az keyvault secret show --vault-name $kvName -n RakutenAffiliateId --query value -o tsv | Out-String).Trim()
  $aiConn = ''
  if ($EnableTelemetry) {
    $aiConn = (Invoke-Az keyvault secret show --vault-name $kvName -n AppInsightsConnectionString --query value -o tsv | Out-String).Trim()
  }

  # ── 3. Azurite ────────────────────────────────────────────────
  if (-not ((Test-Port 10000) -and (Test-Port 10001) -and (Test-Port 10002))) {
    Write-Step 'Azurite を起動'
    if (Get-Command docker -ErrorAction SilentlyContinue) {
      Push-Location $repoRoot
      try { & docker compose up -d azurite | Out-Host } finally { Pop-Location }
    } elseif (Get-Command azurite -ErrorAction SilentlyContinue) {
      $azuriteData = Join-Path $repoRoot '.dev-logs\azurite'
      New-Item -ItemType Directory -Force -Path $azuriteData | Out-Null
      Start-Process -FilePath (Get-Command azurite).Source -ArgumentList "--silent --location `"$azuriteData`" --skipApiVersionCheck" -WindowStyle Hidden | Out-Null
    } else {
      throw 'Azurite が起動しておらず、docker も azurite も見つかりません。'
    }
    $deadline = (Get-Date).AddMinutes(2)
    while (-not ((Test-Port 10000) -and (Test-Port 10001) -and (Test-Port 10002))) {
      if ((Get-Date) -gt $deadline) { throw 'Azurite の起動待ちがタイムアウトしました。' }
      Start-Sleep -Seconds 2
    }
  }

  # ── 4. SQL ファイアウォール + resume ───────────────────────────
  Write-Step "SQL ファイアウォール規則 $fwRule に $publicIp を設定"
  $null = Invoke-Az sql server firewall-rule create -g $rg -s $sqlServer -n $fwRule --start-ip-address $publicIp --end-ip-address $publicIp -o none
  $fwCreated = $true

  $sqlFqdn = "$sqlServer.database.windows.net"
  if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
    Write-Step 'SQL を事前に resume（auto-pause 解除待ち）'
    & sqlcmd -S $sqlFqdn -d $sqlDb --authentication-method ActiveDirectoryAzCli -l 120 -Q 'SELECT 1' | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Warning 'resume 用クエリに失敗（バッチ側のリトライに任せます）。' }
  }

  # ── 5. Functions ホスト起動 ─────────────────────────────────────
  # 環境変数は local.settings.json より優先される（Core Tools は既存の環境変数を上書きしない）。
  $settings = @{
    'AzureWebJobsStorage'                    = 'UseDevelopmentStorage=true'
    'FUNCTIONS_WORKER_RUNTIME'               = 'dotnet-isolated'
    'APPLICATIONINSIGHTS_CONNECTION_STRING'  = $aiConn
    'SqlConnectionString'                    = "Server=tcp:$sqlFqdn,1433;Initial Catalog=$sqlDb;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
    'StorageAccountUri'                      = "https://$storage.blob.core.windows.net"
    'BlobBaseUrl'                            = "https://$storage.blob.core.windows.net/covers"
    'RakutenApplicationId'                   = $rakutenAppId
    'RakutenAccessKey'                       = $rakutenAccessKey
    'RakutenAffiliateId'                     = $rakutenAffId
    # Timer は使わない（このスクリプトが起動タイミングを決める）。% 参照の解決用にダミー値を入れる。
    'DailyBatchCronExpression'               = '0 0 0 1 1 *'
    'WarmupBatchCronExpression'              = '0 0 0 1 1 *'
    'AzureWebJobs.DailyBatchTimer.Disabled'  = 'true'
    'AzureWebJobs.WarmupBatchTimer.Disabled' = 'true'
  }
  foreach ($k in $settings.Keys) { [Environment]::SetEnvironmentVariable($k, $settings[$k], 'Process') }

  $localSettings = Join-Path $batchDir 'local.settings.json'
  if (-not (Test-Path $localSettings)) {
    # Core Tools が言語判定に使う。値は上の環境変数が優先されるのでサンプルのままでよい（gitignore 対象）
    Copy-Item (Join-Path $batchDir 'local.settings.json.sample') $localSettings
  }

  $funcPath = (Get-Command func).Source
  Write-Step "Functions ホストを起動（port $Port）"
  $funcProc = Start-Process -FilePath $funcPath -ArgumentList "start --port $Port" -WorkingDirectory $batchDir `
    -RedirectStandardOutput $hostLog -RedirectStandardError $hostErrLog -WindowStyle Hidden -PassThru

  $base = "http://localhost:$Port"
  $deadline = (Get-Date).AddMinutes(10)   # 初回はビルドが走るため長め
  while ($true) {
    if ($funcProc.HasExited) { throw "Functions ホストが終了しました（$hostLog / $hostErrLog を確認）。" }
    try {
      $st = Invoke-RestMethod -Uri "$base/admin/host/status" -TimeoutSec 5
      if ($st.state -eq 'Running') { break }
    } catch { }
    if ((Get-Date) -gt $deadline) { throw 'Functions ホストの起動待ちがタイムアウトしました。' }
    Start-Sleep -Seconds 5
  }

  # ── 6. オーケストレーション開始 → 完了待ち ──────────────────────
  Write-Step 'DailyFetchOrchestrator を開始'
  $resp = Invoke-WebRequest -Method Post -Uri "$base/api/batch/trigger" -UseBasicParsing -TimeoutSec 60
  $instanceId = $resp.Content.Trim().Trim('"')
  if ([string]::IsNullOrWhiteSpace($instanceId)) { throw 'instanceId を取得できませんでした。' }
  Write-Step "instanceId: $instanceId"

  $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
  $status = $null
  while ($true) {
    Start-Sleep -Seconds 30
    if ($funcProc.HasExited) { throw 'Functions ホストが実行中に終了しました。' }
    try {
      $status = Invoke-RestMethod -Uri "$base/runtime/webhooks/durabletask/instances/$($instanceId)?showInput=false" -TimeoutSec 30
    } catch { Write-Warning "状態取得に失敗: $($_.Exception.Message)"; continue }
    Write-Step "status: $($status.runtimeStatus)"
    if ($status.runtimeStatus -in @('Completed', 'Failed', 'Terminated', 'Canceled')) { break }
    if ((Get-Date) -gt $deadline) { throw "タイムアウト（$TimeoutMinutes 分）。instanceId=$instanceId" }
  }

  if ($status.runtimeStatus -ne 'Completed') {
    throw "バッチが $($status.runtimeStatus) で終了しました。output=$($status.output | ConvertTo-Json -Compress -Depth 5)"
  }
  $summary = "ComiCal バッチ完了 (BatchRunId=$($status.output))"
  Write-Step $summary
  $exitCode = 0
  if ($NotifyOnSuccess) { Send-Notify $summary }
}
catch {
  $summary = "ComiCal バッチ失敗: $($_.Exception.Message)"
  Write-Error $summary -ErrorAction Continue
  Send-Notify "$summary`nログ: $logFile"
}
finally {
  if ($funcProc -and -not $funcProc.HasExited) {
    Write-Step 'Functions ホストを停止'
    # func はワーカー（dotnet）を子プロセスとして起動するため、ツリーごと止める
    & taskkill /PID $funcProc.Id /T /F | Out-Null
  }
  if ($fwCreated) {
    Write-Step "SQL ファイアウォール規則 $fwRule を削除"
    & az sql server firewall-rule delete -g $rg -s $sqlServer -n $fwRule -o none 2>&1 | Out-Null
  }
  foreach ($k in 'RakutenApplicationId', 'RakutenAccessKey', 'RakutenAffiliateId', 'APPLICATIONINSIGHTS_CONNECTION_STRING') {
    [Environment]::SetEnvironmentVariable($k, $null, 'Process')
  }
  Remove-Item -Path $lockFile -ErrorAction SilentlyContinue
  Stop-Transcript | Out-Null
}
exit $exitCode
