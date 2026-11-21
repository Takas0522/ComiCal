<#
.SYNOPSIS
  Phase 0: 即効性のある設定変更（停止を伴わない・何度実行しても同じ結果）。
  IaC（infra/）の最終形と同じ値を実体へ先行適用する。

  - SQL (prod): min vCores 0.5 / auto-pause 15 分 / maxSize 32 GB        … data.bicep と同値
  - Log Analytics (prod): 保持 30 日 / 日次上限 0.2 GB                    … observability.bicep / prod.bicepparam と同値
  - Functions API (prod): 最大インスタンス数 10                           … app.bicep と同値
  - 予算: cmcl-prod-jpe-budget（月 3,000 円, 80%/100%/予測100%）         … modules/budget.bicep をそのままデプロイ
#>
[CmdletBinding()]
param([switch]$Yes, [int]$BudgetAmount = 3000, [string]$BudgetEmail = '')
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-01'
try {
  Assert-AzLogin

  Write-Step "SQL $($Names.SqlDb): min 0.5 vCore / auto-pause 15 分"
  $null = Invoke-Az sql db update -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb `
    --min-capacity 0.5 --auto-pause-delay 15 -o none
  Write-Done 'SQL 設定を更新'

  Write-Step "Log Analytics $($Names.Law): 保持 30 日 / 日次上限 0.2 GB"
  $null = Invoke-Az monitor log-analytics workspace update -g $Names.Rg -n $Names.Law --retention-time 30 --quota 0.2 -o none
  Write-Done 'Log Analytics を更新'

  Write-Step "Functions $($Names.FuncApi): 最大インスタンス数 10"
  $null = Invoke-Az functionapp scale config set -g $Names.Rg -n $Names.FuncApi --maximum-instance-count 10 -o none
  Write-Done 'スケール上限を更新'

  Write-Step '予算アラート（infra/modules/budget.bicep）をデプロイ'
  $agId = (Invoke-Az monitor action-group show -g $Names.Rg -n $Names.ActionGroup --query id -o tsv | Out-String).Trim()
  $emailList = @()
  if (-not [string]::IsNullOrWhiteSpace($BudgetEmail)) { $emailList = @($BudgetEmail) }
  # PowerShell 5.1 ではネイティブコマンドに JSON を渡すと引用符が壊れるため、パラメーターファイル経由で渡す
  $paramFile = Join-Path $env:TEMP 'comical-budget.parameters.json'
  $pjson = @{
    '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters = @{
      prefix        = @{ value = 'cmcl' }
      env           = @{ value = 'prod' }
      amount        = @{ value = $BudgetAmount }
      startDate     = @{ value = '2026-11-01' }
      actionGroupId = @{ value = $agId }
      contactEmails = @{ value = $emailList }
    }
  } | ConvertTo-Json -Depth 5
  [IO.File]::WriteAllText($paramFile, $pjson, (New-Object Text.UTF8Encoding($false)))
  $null = Invoke-Az deployment group create -g $Names.Rg -n "budget-$(Get-Date -Format yyyyMMddHHmmss)" `
    -f (Join-Path $RepoRoot 'infra\modules\budget.bicep') -p "@$paramFile" -o none
  Remove-Item $paramFile -ErrorAction SilentlyContinue
  Write-Done "予算 cmcl-prod-jpe-budget（$BudgetAmount 円/月）"

  Write-Host ''
  Write-Host 'Phase 0 完了。Cost Management の「異常アラート」はポータルの「コストのアラート」から任意で有効化してください。' -ForegroundColor Green
}
finally { Stop-Transcript | Out-Null; Write-Host "ログ: $log" }
