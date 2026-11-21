<#
.SYNOPSIS
  Phase 3b: Azure 上の Batch 関連リソースを削除する（apply-04 でローカル実行が安定してから実施）。

  - Function App cmcl-prod-jpe-func-batch と FC1 プラン cmcl-prod-jpe-plan-batch
  - Batch 失敗アラート cmcl-prod-jpe-alert-batch-failed（Batch がなくなるため無意味。月 約 235 円）
  - Storage: app-package-batch コンテナ、Durable タスクハブ ComiCalBatch のテーブル / キュー / Blob コンテナ
  - DB ユーザー cmcl-prod-jpe-func-batch（Batch の Managed Identity）
#>
[CmdletBinding()]
param([switch]$Yes)
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-05'
$rule = $null
$prevAdmin = $null
try {
  Assert-AzLogin
  if (-not (Confirm-Action 'ローカル実行のバッチが成功していることを確認済みですか？（/admin/batch-runs）')) { throw '中止しました。' }

  if (Test-AzResource 'Microsoft.Web/sites' $Names.Rg $Names.FuncBatch) {
    Write-Step "Function App $($Names.FuncBatch) を削除"
    $null = Invoke-Az functionapp delete -g $Names.Rg -n $Names.FuncBatch -o none
    Write-Done $Names.FuncBatch
  } else { Write-Skip "$($Names.FuncBatch) は既にありません" }
  if (Test-AzResource 'Microsoft.Web/serverFarms' $Names.Rg $Names.PlanBatch) {
    $null = Invoke-Az functionapp plan delete -g $Names.Rg -n $Names.PlanBatch --yes -o none
    Write-Done $Names.PlanBatch
  }

  if (Test-AzResource 'Microsoft.Insights/scheduledQueryRules' $Names.Rg $Names.AlertBatch) {
    Write-Step "アラート $($Names.AlertBatch) を削除"
    $null = Invoke-Az resource delete -g $Names.Rg -n $Names.AlertBatch --resource-type Microsoft.Insights/scheduledQueryRules -o none
    Write-Done $Names.AlertBatch
  }

  Write-Step 'Storage の Batch 用データを削除（アカウントキーで操作）'
  # キーはコマンドライン引数ではなく環境変数で渡す（エラーメッセージやログに出さないため）
  $env:AZURE_STORAGE_ACCOUNT = $Names.Storage
  $env:AZURE_STORAGE_KEY = (Invoke-Az storage account keys list -g $Names.Rg -n $Names.Storage --query '[0].value' -o tsv | Out-String).Trim()
  $sa = @()   # AZURE_STORAGE_ACCOUNT / AZURE_STORAGE_KEY を使う
  $containers = Invoke-Az storage container list @sa --query '[].name' -o tsv
  foreach ($c in $containers) {
    if ($c -eq 'app-package-batch' -or $c -like 'comicalbatch-*') {
      $null = Invoke-Az storage container delete @sa -n $c -o none; Write-Done "container: $c"
    }
  }
  $queues = Invoke-Az storage queue list @sa --query '[].name' -o tsv
  foreach ($q in $queues) {
    if ($q -like 'comicalbatch-*') { $null = Invoke-Az storage queue delete @sa -n $q -o none; Write-Done "queue: $q" }
  }
  $tables = Invoke-Az storage table list @sa --query '[].name' -o tsv
  foreach ($t in $tables) {
    if ($t -like 'ComiCalBatch*') { $null = Invoke-Az storage table delete @sa -n $t -o none; Write-Done "table: $t" }
  }

  Write-Step "DB ユーザー $($Names.FuncBatch) を削除"
  $prevAdmin = Use-SqlEntraAdmin
  $rule = Add-TempFirewallRule (Get-PublicIp)
  Invoke-SqlAzCli $Names.SqlDb "IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$($Names.FuncBatch)') DROP USER [$($Names.FuncBatch)];"
  Write-Done 'DROP USER'
}
finally {
  Remove-Item Env:AZURE_STORAGE_KEY -ErrorAction SilentlyContinue
  Remove-Item Env:AZURE_STORAGE_ACCOUNT -ErrorAction SilentlyContinue
  if ($rule) { Remove-TempFirewallRule $rule }
  Restore-SqlEntraAdmin $prevAdmin
  Stop-Transcript | Out-Null; Write-Host "ログ: $log"
}
