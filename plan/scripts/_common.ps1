# ComiCal コスト削減: apply-*.ps1 共通ヘルパー（dot-source して使う）
$ErrorActionPreference = 'Stop'

$Script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$Script:Names = [ordered]@{
  Rg          = 'cmcl-prod-jpe-rg'
  DevRg       = 'cmcl-dev-jpe-rg'
  SqlServer   = 'cmcl-prod-jpe-sql'
  SqlDb       = 'cmcl-prod-jpe-sqldb'
  Storage     = 'cmclprodjpest'
  Kv          = 'cmcl-prod-jpe-kv'
  DevKv       = 'cmcl-dev-jpe-kv'
  AppCfg      = 'cmcl-prod-jpe-appcfg'
  DevAppCfg   = 'cmcl-dev-jpe-appcfg'
  FuncApi     = 'cmcl-prod-jpe-func-api'
  FuncBatch   = 'cmcl-prod-jpe-func-batch'
  PlanBatch   = 'cmcl-prod-jpe-plan-batch'
  Law         = 'cmcl-prod-jpe-log'
  ActionGroup = 'cmcl-prod-jpe-ag'
  AlertBatch  = 'cmcl-prod-jpe-alert-batch-failed'
  Vnet        = 'cmcl-prod-jpe-vnet'
  Nat         = 'cmcl-prod-jpe-nat'
  Pip         = 'cmcl-prod-jpe-pip-egress'
  Swa         = 'cmcl-prod-jpe-swa'
  BatchTask   = 'ComiCal-DailyBatch'
  Location    = 'japaneast'
  GitHubRepo  = 'Takas0522/ComiCal'
}

$logDir = Join-Path $Script:RepoRoot '.secrets\apply-logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Write-Step([string]$msg) { Write-Host ''; Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Done([string]$msg) { Write-Host "    OK: $msg" -ForegroundColor Green }
function Write-Skip([string]$msg) { Write-Host "    SKIP: $msg" -ForegroundColor DarkGray }

function Confirm-Action([string]$msg) {
  # -Yes スイッチ（呼び出し側の $Yes）で確認を省略
  if ($Script:AssumeYes) { return $true }
  $ans = Read-Host "$msg [y/N]"
  return ($ans -match '^(y|yes)$')
}

function Invoke-Az {
  $out = & az @args 2>&1
  if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') が失敗しました:`n$out" }
  return $out
}

function Test-AzResource([string]$type, [string]$rg, [string]$name) {
  $id = & az resource list -g $rg --resource-type $type --query "[?name=='$name'].id | [0]" -o tsv 2>$null
  return -not [string]::IsNullOrWhiteSpace($id)
}

function Assert-AzLogin {
  $acct = Invoke-Az account show -o json | Out-String | ConvertFrom-Json
  Write-Host ("サブスクリプション: {0} ({1}) / ユーザー: {2}" -f $acct.name, $acct.id, $acct.user.name)
  if (-not (Confirm-Action 'このサブスクリプションで実行しますか？')) { throw '中止しました。' }
}

function Get-PublicIp { (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).ToString().Trim() }

function Get-SignedInObjectId { (Invoke-Az ad signed-in-user show --query id -o tsv | Out-String).Trim() }

# 実行中ユーザーを一時的に SQL Server の Entra 管理者にする。
# 戻り値（変更前の管理者。変更しなかった場合は $null）を Restore-SqlEntraAdmin に渡して finally で必ず戻すこと。
# （apply-07 完了まで cd-prod.yml を実行しないため、「次回 CD で戻る」に頼ると数日〜数週間個人が管理者のまま残る）
function Use-SqlEntraAdmin {
  $me = Invoke-Az ad signed-in-user show -o json | Out-String | ConvertFrom-Json
  $prev = Invoke-Az sql server ad-admin list -g $Names.Rg -s $Names.SqlServer -o json | Out-String | ConvertFrom-Json | Select-Object -First 1
  if ($prev -and $prev.sid -eq $me.id) { Write-Skip "SQL Server の Entra 管理者は既に $($me.userPrincipalName) です"; return $null }
  if (-not $prev) { throw 'SQL Server の Entra 管理者が取得できません。中断します（復元先が不明なため）。' }
  Write-Step "SQL Server の Entra 管理者を一時的に $($me.userPrincipalName) に設定（終了時に $($prev.login) へ戻します）"
  $null = Invoke-Az sql server ad-admin create -g $Names.Rg -s $Names.SqlServer --display-name $me.userPrincipalName --object-id $me.id -o none
  return $prev
}

function Restore-SqlEntraAdmin($prev) {
  if (-not $prev) { return }
  Write-Step "SQL Server の Entra 管理者を $($prev.login) に戻す"
  & az sql server ad-admin create -g $Names.Rg -s $Names.SqlServer --display-name $prev.login --object-id $prev.sid -o none 2>&1 | Out-Host
  if ($LASTEXITCODE -ne 0) {
    Write-Warning "Entra 管理者の復元に失敗しました。手動で実行してください: az sql server ad-admin create -g $($Names.Rg) -s $($Names.SqlServer) --display-name '$($prev.login)' --object-id $($prev.sid)"
  }
}

# ローカル Batch のスケジュールタスク（tools/register-batch-task.ps1 の既定名）。
# 先に無効化してから実行中でないことを確認する（確認 → 無効化の間にタスクが起動する競合を防ぐ）。
# 実行中なら元の状態に戻して中止する。手動実行（タスク外）も検出するため Batch のプロセスも確認する。
function Disable-BatchTask {
  $t = Get-ScheduledTask -TaskName $Names.BatchTask -ErrorAction SilentlyContinue
  $disabledHere = $false
  if ($t -and $t.State -ne 'Disabled') {
    $null = Disable-ScheduledTask -TaskName $Names.BatchTask
    $disabledHere = $true
  }
  $running = @()
  if ($t -and (Get-ScheduledTask -TaskName $Names.BatchTask).State -eq 'Running') { $running += "タスク $($Names.BatchTask)" }
  # 現行の暫定実装は func ホスト（tools/run-batch-local.ps1）、PR-3 以降は ComiCal.Batch.exe
  $procs = Get-Process -Name 'ComiCal.Batch', 'func' -ErrorAction SilentlyContinue
  if ($procs) { $running += "プロセス $(($procs.Name | Sort-Object -Unique) -join ', ')" }
  if ($running) {
    if ($disabledHere) { $null = Enable-ScheduledTask -TaskName $Names.BatchTask -ErrorAction Continue }
    throw "Batch が実行中です（$($running -join ' / ')）。完了を待ってから再実行してください。"
  }
  if ($disabledHere) { Write-Done "タスク $($Names.BatchTask) を一時無効化" }
  return $disabledHere
}
function Enable-BatchTask([bool]$wasDisabled) {
  if (-not $wasDisabled) { return }
  $null = Enable-ScheduledTask -TaskName $Names.BatchTask -ErrorAction Continue
  Write-Host "タスク $($Names.BatchTask) を再有効化しました"
}

function Add-TempFirewallRule([string]$ip) {
  $rule = "apply-script-$($env:COMPUTERNAME)".ToLower()
  $null = Invoke-Az sql server firewall-rule create -g $Names.Rg -s $Names.SqlServer -n $rule --start-ip-address $ip --end-ip-address $ip -o none
  return $rule
}
function Remove-TempFirewallRule([string]$rule) {
  & az sql server firewall-rule delete -g $Names.Rg -s $Names.SqlServer -n $rule -o none 2>&1 | Out-Null
}

function Assert-Command([string]$name, [string]$hint) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw "$name が見つかりません。$hint" }
}

# go-sqlcmd（Entra 認証は az のセッションを利用）で T-SQL を実行
function Invoke-SqlAzCli([string]$database, [string]$query) {
  Assert-Command 'sqlcmd' 'go-sqlcmd をインストールしてください: winget install sqlcmd'
  $tmp = [IO.Path]::GetTempFileName()
  try {
    [IO.File]::WriteAllText($tmp, $query, (New-Object Text.UTF8Encoding($false)))
    & sqlcmd -S "$($Names.SqlServer).database.windows.net" -d $database --authentication-method ActiveDirectoryAzCli -l 120 -b -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd が失敗しました（$database）。" }
  } finally { Remove-Item $tmp -ErrorAction SilentlyContinue }
}

# cd-prod.yml の Grant ステップと同じ T-SQL（SID 指定で CREATE USER + ロール付与）
function Get-GrantSql([string]$name, [string]$guid) {
@"
SET NOCOUNT ON;
DECLARE @name sysname          = N'$name';
DECLARE @id   uniqueidentifier = N'$guid';
DECLARE @sid  varbinary(16)    = CONVERT(varbinary(16), @id);
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @name AND sid <> @sid)
BEGIN
    DECLARE @drop nvarchar(max) = N'DROP USER ' + QUOTENAME(@name);
    EXEC sp_executesql @drop;
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @name)
BEGIN
    DECLARE @create nvarchar(max) = N'CREATE USER ' + QUOTENAME(@name)
        + N' WITH SID = ' + CONVERT(nvarchar(100), @sid, 1) + N', TYPE = E';
    EXEC sp_executesql @create;
END;
IF ISNULL(IS_ROLEMEMBER(N'db_datareader', @name), 0) = 0
BEGIN
    DECLARE @r nvarchar(max) = N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@name);
    EXEC sp_executesql @r;
END;
IF ISNULL(IS_ROLEMEMBER(N'db_datawriter', @name), 0) = 0
BEGIN
    DECLARE @w nvarchar(max) = N'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@name);
    EXEC sp_executesql @w;
END;
DECLARE @x nvarchar(max) = N'GRANT EXECUTE TO ' + QUOTENAME(@name);
EXEC sp_executesql @x;
"@
}

function Start-ApplyLog([string]$name) {
  $f = Join-Path $logDir ("{0}-{1:yyyyMMdd-HHmmss}.log" -f $name, (Get-Date))
  Start-Transcript -Path $f | Out-Null
  return $f
}
