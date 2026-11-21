<#
.SYNOPSIS
  Phase 3a: バッチをローカル実行に切り替える準備。Azure の Batch Function App は「停止」するだけ（削除は apply-05）。

  1. 実行ユーザーに Storage Blob Data Contributor / Key Vault Secrets User を付与
     （infra/modules/batch-operator.bicep をそのままデプロイ。main.bicep と同じロール割り当て名になる）
  2. 実行ユーザーを DB ユーザー comical-batch-operator として作成（cd-prod.yml の Grant と同じ T-SQL）
  3. GitHub 変数 BATCH_OPERATOR_PRINCIPAL_ID を設定（gh CLI がある場合。次回以降の本番デプロイで維持される）
  4. Azure 側 Batch Function App を停止（楽天 API の二重呼び出し防止）
  5. 楽天ウェブサービスのアプリ設定に登録する自宅のグローバル IP を表示
  6. （任意）tools/run-batch-local.ps1 を 1 回実行して動作確認 → タスク スケジューラ登録
#>
[CmdletBinding()]
param([switch]$Yes, [switch]$SkipTestRun)
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-04'
$rule = $null
try {
  Assert-AzLogin
  foreach ($c in 'func', 'dotnet') { Assert-Command $c 'ローカル実行には Azure Functions Core Tools v4 と .NET 10 SDK が必要です。' }
  Assert-Command 'sqlcmd' 'go-sqlcmd をインストールしてください: winget install sqlcmd'

  $oid = Get-SignedInObjectId
  Write-Host "実行ユーザーの objectId: $oid"

  Write-Step 'RBAC（infra/modules/batch-operator.bicep）'
  $null = Invoke-Az deployment group create -g $Names.Rg -n "batch-operator-$(Get-Date -Format yyyyMMddHHmmss)" `
    -f (Join-Path $RepoRoot 'infra\modules\batch-operator.bicep') `
    -p storageAccountName=$($Names.Storage) keyVaultName=$($Names.Kv) principalId=$oid -o none
  Write-Done 'Storage Blob Data Contributor / Key Vault Secrets User'

  Write-Step 'DB ユーザー comical-batch-operator'
  Use-SqlEntraAdmin
  $rule = Add-TempFirewallRule (Get-PublicIp)
  Invoke-SqlAzCli $Names.SqlDb (Get-GrantSql 'comical-batch-operator' $oid)
  Write-Done 'db_datareader / db_datawriter / EXECUTE'

  if (Get-Command gh -ErrorAction SilentlyContinue) {
    & gh variable set BATCH_OPERATOR_PRINCIPAL_ID --env prod --body $oid -R $Names.GitHubRepo 2>&1 | Out-Host
    Write-Done 'GitHub 変数 BATCH_OPERATOR_PRINCIPAL_ID（environment: prod）'
  } else {
    Write-Skip "gh CLI がありません。GitHub の Settings → Environments → prod → Variables に BATCH_OPERATOR_PRINCIPAL_ID=$oid を追加してください"
  }

  if (Test-AzResource 'Microsoft.Web/sites' $Names.Rg $Names.FuncBatch) {
    Write-Step "Azure 側 $($Names.FuncBatch) を停止"
    $null = Invoke-Az functionapp stop -g $Names.Rg -n $Names.FuncBatch -o none
    Write-Done '停止（削除は apply-05 で）'
  }

  $ip = Get-PublicIp
  Write-Host ''
  Write-Host "★ 楽天ウェブサービスのアプリ設定で、許可 IP アドレスに $ip を追加してください。" -ForegroundColor Yellow
  Write-Host "  （tools/run-batch-local.ps1 に照合させるなら: setx COMICAL_RAKUTEN_ALLOWED_IP $ip）" -ForegroundColor Yellow

  if (-not $SkipTestRun -and (Confirm-Action '楽天の設定を終えたら、ローカルでバッチを 1 回試走しますか？（数十分かかります）')) {
    if ($rule) { Remove-TempFirewallRule $rule; $rule = $null }
    & (Join-Path $RepoRoot 'tools\run-batch-local.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'ローカル試走が失敗しました。.dev-logs\batch のログを確認してください。' }
    Write-Done 'ローカル試走成功（管理画面 /admin/batch-runs でも確認できます）'
    if (Confirm-Action 'タスク スケジューラに毎日 03:00 で登録しますか？') {
      & (Join-Path $RepoRoot 'tools\register-batch-task.ps1')
    }
  }
}
finally {
  if ($rule) { Remove-TempFirewallRule $rule }
  Stop-Transcript | Out-Null; Write-Host "ログ: $log"
}
