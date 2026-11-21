<#
.SYNOPSIS
  tools/run-batch-local.ps1 を Windows タスク スケジューラに毎日実行するタスクとして登録する。

.NOTES
  - 「ユーザーがログオンしているときのみ実行」で登録する（az の資格情報キャッシュは DPAPI で
    ユーザー単位に暗号化されているため、ログオフ状態や S4U では読めない）。画面ロック中は実行される。
  - スリープ解除して実行 / 開始時刻を逃した場合は可能になり次第実行、を有効化する。
  - 解除: Unregister-ScheduledTask -TaskName ComiCal-DailyBatch -Confirm:$false
#>
[CmdletBinding()]
param(
  [string]$TaskName = 'ComiCal-DailyBatch',
  [string]$At = '03:00',
  [string]$ExtraArguments = ''
)
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'run-batch-local.ps1'
if (-not (Test-Path $script)) { throw "$script が見つかりません。" }

$pwsh = (Get-Command pwsh -ErrorAction SilentlyContinue)
$exe = if ($pwsh) { $pwsh.Source } else { (Get-Command powershell).Source }

$action   = New-ScheduledTaskAction -Execute $exe `
  -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$script`" $ExtraArguments" `
  -WorkingDirectory (Split-Path -Parent $PSScriptRoot)
$trigger  = New-ScheduledTaskTrigger -Daily -At $At
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -WakeToRun `
  -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
  -ExecutionTimeLimit (New-TimeSpan -Hours 5) -MultipleInstances IgnoreNew
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
  -Principal $principal -Description 'ComiCal 日次収集バッチ（楽天 Books API → Azure SQL / Blob）' -Force | Out-Null

Write-Host "登録しました: $TaskName（毎日 $At）。手動実行: Start-ScheduledTask -TaskName $TaskName"
