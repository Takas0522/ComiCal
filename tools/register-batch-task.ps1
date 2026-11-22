<#
.SYNOPSIS
  ComiCal.Batch.exe run を Windows タスク スケジューラに登録する。

.NOTES
  セキュリティ オプション                     動作と使い分け
  ログオン時のみ（既定、Interactive）         画面ロック中も実行。パスワード保存不要
  ログオンの有無を問わない（Password）         サインアウト中も実行。ユーザープロファイルを読み込む
  ログオンの有無を問わない（S4U）              暗号化された Azure CLI キャッシュを読めないため使用しない

  Azure CLI にログインした Windows ユーザーで登録する。Password 方式は
  -RunWhetherLoggedOn を指定し、同じユーザーの Windows パスワードを入力する。
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low', DefaultParameterSetName = 'Register')]
param(
  [string]$TaskName = 'ComiCal-DailyBatch',
  [Parameter(ParameterSetName = 'Register')][string]$Root,
  [Parameter(ParameterSetName = 'Register')][string]$At = '03:00',
  [Parameter(ParameterSetName = 'Register')][switch]$RunWhetherLoggedOn,
  [Parameter(Mandatory, ParameterSetName = 'Unregister')][switch]$Unregister
)
$ErrorActionPreference = 'Stop'
$taskPath = '\'
if ($Unregister) {
  if ($PSCmdlet.ShouldProcess($TaskName, 'Unregister scheduled task')) {
    Unregister-ScheduledTask -TaskName $TaskName -TaskPath $taskPath -Confirm:$false
  }
  return
}

if (-not $Root) { $Root = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ComiCal\Batch' }
$Root = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Root)
$current = Join-Path $Root 'current'
$exe = Join-Path $current 'ComiCal.Batch.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
  throw "$exe が見つかりません。先に tools/publish-batch.ps1 を実行してください。"
}
if (-not $PSCmdlet.ShouldProcess($TaskName, 'Register scheduled task')) { return }

$action = New-ScheduledTaskAction -Execute $exe -Argument 'run' -WorkingDirectory $current
$trigger = New-ScheduledTaskTrigger -Daily -At $At
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -WakeToRun `
  -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
  -ExecutionTimeLimit (New-TimeSpan -Hours 5) -MultipleInstances IgnoreNew
$user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$registration = @{
  TaskName = $TaskName
  TaskPath = $taskPath
  Action = $action
  Trigger = $trigger
  Settings = $settings
  Description = 'ComiCal 日次収集バッチ（楽天 Books API → Azure SQL / Blob）'
  Force = $true
}

if ($RunWhetherLoggedOn) {
  $credential = Get-Credential -UserName $user -Message 'Azure CLI にログインした Windows ユーザーのパスワードを入力してください。'
  if (-not $credential -or $credential.UserName -ne $user) { throw "$user の資格情報が必要です。" }
  # Register-ScheduledTask selects Password logon when supplied with User and Password.
  Register-ScheduledTask @registration -User $user -Password ($credential.GetNetworkCredential().Password) -RunLevel Limited | Out-Null
} else {
  $principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
  Register-ScheduledTask @registration -Principal $principal | Out-Null
}
Write-Information -InformationAction Continue "登録しました。$TaskName（毎日 $At）。手動実行は Start-ScheduledTask -TaskName '$TaskName'。"

# Query hidden settings too and identify RTCWAKE by GUID, independently of OS language.
$query = 'powercfg /qh SCHEME_CURRENT SUB_SLEEP'
$change = @'
電源オプションの詳細設定 → スリープ → スリープ解除タイマーの許可 → 有効。
項目が表示されない場合は、管理者権限で使う電源条件に応じて以下を実行し、電源プランを再適用してください。
  powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 1
  powercfg /setdcvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 1
  powercfg /setactive SCHEME_CURRENT
'@
try {
  $output = & powercfg /qh SCHEME_CURRENT SUB_SLEEP
  if ($LASTEXITCODE -ne 0) { throw '電源プランを取得できませんでした。' }
  $guidPattern = '[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}'
  $wakeGuid = 'bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d'
  $readingWakeSetting = $false
  $indexes = @(foreach ($line in $output) {
    if ($line -match $guidPattern) {
      if ($readingWakeSetting) { break }
      $readingWakeSetting = $Matches[0] -eq $wakeGuid
    } elseif ($readingWakeSetting -and $line -match '0x([0-9a-f]{8})\s*$') {
      [Convert]::ToInt32($Matches[1], 16)
    }
  })
  if ($indexes.Count -ne 2) { throw 'RTCWAKE の AC / DC 設定値を取得できませんでした。' }
  $states = @{ 0 = '無効'; 1 = '有効'; 2 = '重要なタイマーのみ' }
  $ac, $dc = $indexes
  if (-not $states.ContainsKey($ac) -or -not $states.ContainsKey($dc)) { throw "AC=$ac DC=$dc" }
  Write-Information -InformationAction Continue "スリープ解除タイマー（現在の電源プラン） AC=$($states[$ac]) / バッテリー=$($states[$dc])"
  if ($ac -ne 1 -or $dc -ne 1) { Write-Warning "使う電源条件でスリープ解除タイマーを有効にしてください。$change" }
} catch {
  Write-Warning "スリープ解除タイマーの状態を確認できませんでした。手動確認コマンドは $query です。設定場所は $change です。"
}
