<#
.SYNOPSIS
  Phase 3a: Windows のコンソール版 Batch に切り替える。Azure Batch の削除は apply-05 で行う。

  1. 実行ユーザーに RBAC を付与し、SQL の常設規則 batch-operator を現在の IP で作成する。
  2. DB ユーザー comical-batch-operator を作成し、SQL の Entra 管理者を元に戻す。
  3. GitHub の prod 環境変数を設定し、ローカル設定の雛形を作る（既存設定は維持）。
  4. publish → preflight → 楽天の許可 IP 登録を人が確認 → Azure Batch 停止 → run --force。
  5. タスク登録成功で切り替え完了。試走失敗・登録しない・登録失敗時は停止した Azure Batch を再開する。
  -SkipTestRun は手順 3 まで実行する。楽天 IP の確認は -Yes でも省略しない。
#>
[CmdletBinding()]
param([switch]$Yes, [switch]$SkipTestRun, [switch]$AllowDirty)
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-04'
$prevAdmin = $null
$batchStopped = $false
$cutoverDone = $false
try {
  Assert-AzLogin
  Assert-Command 'dotnet' 'Batch の publish には .NET 10 SDK が必要です。'
  Assert-Command 'git' 'Batch の公開世代を識別するために Git が必要です。'
  Assert-Command 'sqlcmd' 'go-sqlcmd をインストールしてください: winget install sqlcmd'

  $oid = Get-SignedInObjectId
  $ip = Get-PublicIp
  Write-Information -InformationAction Continue "実行ユーザーの objectId: $oid"

  Write-Step 'RBAC（infra/modules/batch-operator.bicep）'
  $null = Invoke-Az deployment group create -g $Names.Rg -n "batch-operator-$(Get-Date -Format yyyyMMddHHmmss)" `
    -f (Join-Path $RepoRoot 'infra\modules\batch-operator.bicep') `
    -p storageAccountName=$($Names.Storage) keyVaultName=$($Names.Kv) principalId=$oid -o none
  Write-Done 'Storage Blob Data Contributor / Key Vault Secrets User'

  Write-Step "SQL の常設ファイアウォール規則 batch-operator（$ip）"
  $null = Invoke-Az sql server firewall-rule create -g $Names.Rg -s $Names.SqlServer -n batch-operator `
    --start-ip-address $ip --end-ip-address $ip -o none

  Write-Step 'DB ユーザー comical-batch-operator'
  $prevAdmin = Use-SqlEntraAdmin
  Invoke-SqlAzCli $Names.SqlDb (Get-GrantSql 'comical-batch-operator' $oid)
  Write-Done 'db_datareader / db_datawriter / EXECUTE'
  if ($prevAdmin) {
    Restore-SqlEntraAdmin $prevAdmin -RequireSuccess
    $prevAdmin = $null
  }

  $variables = [ordered]@{ BATCH_OPERATOR_PRINCIPAL_ID = $oid; BATCH_OPERATOR_IP_ADDRESS = $ip }
  $gh = Get-Command gh -ErrorAction SilentlyContinue
  foreach ($variable in $variables.GetEnumerator()) {
    if ($gh) {
      & gh variable set $variable.Key --env prod --body $variable.Value -R $Names.GitHubRepo 2>&1 | Out-Host
      if ($LASTEXITCODE -eq 0) {
        Write-Done "GitHub 変数 $($variable.Key)（environment: prod）"
        continue
      }
    }
    Write-Warning "GitHub の Settings → Environments → prod → Variables に $($variable.Key)=$($variable.Value) を設定してください。"
  }

  $root = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ComiCal\Batch'
  $settingsPath = Join-Path $root 'appsettings.Local.json'
  if (-not (Test-Path -LiteralPath $settingsPath)) {
    $sample = Join-Path $RepoRoot 'src/backend/batch/ComiCal.Batch/appsettings.Local.json.sample'
    $template = Get-Content -LiteralPath $sample -Raw -Encoding UTF8 | ConvertFrom-Json
    # Keep the Batch section and drop unset overrides so the published defaults apply.
    foreach ($section in @($template, $template.Batch)) {
      foreach ($property in @($section.PSObject.Properties)) {
        if ($property.Value -eq '' -and $property.Name -ne 'ExpectedPublicIp') { $section.PSObject.Properties.Remove($property.Name) }
      }
    }
    $template.PSObject.Properties.Remove('Azure')
    $template.Batch.ExpectedPublicIp = $ip
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $stream = [IO.File]::Open($settingsPath, 'CreateNew', 'Write')
    try {
      $json = [Text.Encoding]::UTF8.GetBytes(($template | ConvertTo-Json -Depth 5) + [Environment]::NewLine)
      $stream.Write($json, 0, $json.Length)
    } finally {
      $stream.Dispose()
    }
    Write-Done "ローカル設定を作成しました。$settingsPath"
    Write-Information -InformationAction Continue '必要に応じてローカル設定に通知先とハートビート URL を設定してください。'
  }

  Write-Information -InformationAction Continue "楽天ウェブサービスのアプリ設定で、許可 IP アドレスに $ip を追加してください。"
  if ($SkipTestRun) {
    Write-Skip '試走を省略しました。切り替えるときは -SkipTestRun なしで再実行してください。'
    return
  }

  & (Join-Path $RepoRoot 'tools/publish-batch.ps1') -Root $root -TaskName $Names.BatchTask -AllowDirty:$AllowDirty
  $exe = Join-Path $root 'current\ComiCal.Batch.exe'
  & $exe preflight
  if ($LASTEXITCODE -ne 0) { throw "preflight が失敗しました（終了コード $LASTEXITCODE）。設定と $root\logs を確認してください。" }

  # 楽天側の IP 登録は手作業のため、-Yes でも人の確認を取る。
  $Script:AssumeYes = $false
  if (-not (Confirm-Action "楽天に $ip を登録しましたか？ Azure 側 Batch を停止し、ローカルで試走します")) { return }

  if (Test-AzResource 'Microsoft.Web/sites' $Names.Rg $Names.FuncBatch) {
    $state = (Invoke-Az functionapp show -g $Names.Rg -n $Names.FuncBatch --query state -o tsv | Out-String).Trim()
    if ($state -eq 'Running') {
      Write-Step "Azure 側 $($Names.FuncBatch) を停止（切り替え未完了なら再開します）"
      $null = Invoke-Az functionapp stop -g $Names.Rg -n $Names.FuncBatch -o none
      $batchStopped = $true
    }
  }
  & $exe run --force
  if ($LASTEXITCODE -ne 0) { throw "ローカル試走が失敗しました（終了コード $LASTEXITCODE）。$root\logs を確認してください。" }
  Write-Done 'ローカル試走成功（管理画面 /admin/batch-runs でも確認できます）'

  if (Confirm-Action 'タスク スケジューラに毎日 03:00 で登録しますか？（登録しない場合は Azure 側 Batch を再開します）') {
    & (Join-Path $RepoRoot 'tools/register-batch-task.ps1') -Root $root -TaskName $Names.BatchTask
    $task = Get-ScheduledTask -TaskName $Names.BatchTask -TaskPath '\'
    if ($task.State -eq 'Disabled' -or $task.Actions.Execute -ne $exe -or $task.Actions.Arguments -ne 'run') {
      throw "タスク $($Names.BatchTask) の登録を確認できません。"
    }
    $cutoverDone = $true
    Write-Done 'タスク登録済み。Azure 側 Batch は停止したまま（削除は apply-05 で）'
  }
}
finally {
  try {
    if ($batchStopped -and -not $cutoverDone) {
      Write-Step "ロールバック: Azure 側 $($Names.FuncBatch) を再開"
      $null = Invoke-Az functionapp start -g $Names.Rg -n $Names.FuncBatch -o none
    }
  } finally {
    Restore-SqlEntraAdmin $prevAdmin
    Stop-Transcript | Out-Null
    Write-Information -InformationAction Continue "ログ: $log"
  }
}
