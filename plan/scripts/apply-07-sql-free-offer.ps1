<#
.SYNOPSIS
  Phase 5: 本番 DB を Azure SQL Database 無料オファー（毎月 100,000 vCore 秒 / 32 GB 無料）の DB に移行する。
  既存 DB は無料オファーに変換できないため「旧 DB をリネーム → 同名で無料オファー DB を作成 → bacpac で移送」。
  接続文字列（DB 名）は変わらない。data.bicep / prod.bicepparam の sqlUseFreeLimit=true と一致させる。

  ダウンタイム: API を停止している間（DB サイズ次第で 10〜30 分程度）。

  手順:
    1. 前提確認（無料オファー DB が未使用か、sqlpackage / sqlcmd があるか）
    2. API Function App を停止し、ローカル Batch のスケジュールタスクを一時無効化（書き込みを止める）
    3. bacpac をエクスポート（.secrets\sql-backup\ に保存）
    4. 旧 DB を <名前>-old にリネーム
    5. 同名で無料オファー DB を作成（BillOverUsage, min 0.5 / pause 15 分 / 32 GB / LRS / Japanese_CI_AS）
    6. bacpac をインポート
    7. API MI と comical-batch-operator の DB 権限を付与
    8. API を起動してスモークテスト
  API 再開前の失敗時は自動で旧 DB の名前を戻す（API 再開後は新 DB に書き込みが入りうるため自動では戻さない）。
  SQL の Entra 管理者は一時的に実行ユーザーに切り替え、終了時に元へ戻す。旧 DB の削除は確認後に -DeleteOldDatabase で別途実行。
#>
[CmdletBinding()]
param([switch]$Yes, [switch]$DeleteOldDatabase)
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-07'
$oldDb = "$($Names.SqlDb)-old"
$rule = $null
$renamed = $false
$created = $false
$apiStopped = $false
$writesResumed = $false
$prevAdmin = $null
$taskDisabled = $false

try {
  Assert-AzLogin

  if ($DeleteOldDatabase) {
    $exists = & az sql db show -g $Names.Rg -s $Names.SqlServer -n $oldDb --query name -o tsv 2>$null
    if (-not $exists) { Write-Skip "$oldDb はありません"; return }
    if (-not (Confirm-Action "旧 DB $oldDb を削除します（bacpac は .secrets\sql-backup に残っています）。続行しますか？")) { throw '中止しました。' }
    $null = Invoke-Az sql db delete -g $Names.Rg -s $Names.SqlServer -n $oldDb --yes -o none
    Write-Done "削除: $oldDb"
    return
  }

  Assert-Command 'sqlpackage' 'dotnet tool install -g microsoft.sqlpackage でインストールしてください。'
  Assert-Command 'sqlcmd' 'winget install sqlcmd でインストールしてください。'

  Write-Step '前提確認'
  $cur = Invoke-Az sql db show -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb -o json | Out-String | ConvertFrom-Json
  if ($cur.useFreeLimit -eq $true) { Write-Skip "$($Names.SqlDb) は既に無料オファーです"; return }
  $freeDbs = @()
  foreach ($srv in (Invoke-Az sql server list --query '[].{n:name,g:resourceGroup}' -o json | Out-String | ConvertFrom-Json)) {
    $freeDbs += Invoke-Az sql db list -g $srv.g -s $srv.n --query "[?useFreeLimit].name" -o tsv
  }
  $freeDbs = @($freeDbs | Where-Object { $_ })
  if ($freeDbs.Count -ge 10) { throw '無料オファー DB が既に 10 個あります。' }
  if ($freeDbs.Count -gt 0) { Write-Host "既存の無料オファー DB: $($freeDbs -join ', ')（リージョンはサブスクリプション内で共通になります）" }
  $oldExists = & az sql db show -g $Names.Rg -s $Names.SqlServer -n $oldDb --query name -o tsv 2>$null
  if ($oldExists) { throw "$oldDb が既に存在します。前回の移行の残りを確認してください。" }
  Write-Host ("現在: {0} / max {1} vCore / min {2} / {3} GB" -f $cur.currentServiceObjectiveName, $cur.capacity, $cur.minCapacity, [math]::Round($cur.maxSizeBytes / 1GB))
  if (-not (Confirm-Action 'API を停止して移行を開始しますか？（サイトは移行中アクセス不可）')) { throw '中止しました。' }

  $prevAdmin = Use-SqlEntraAdmin
  $rule = Add-TempFirewallRule (Get-PublicIp)

  Write-Step "API $($Names.FuncApi) を停止"
  $null = Invoke-Az functionapp stop -g $Names.Rg -n $Names.FuncApi -o none
  $apiStopped = $true
  $taskDisabled = Disable-BatchTask

  Write-Step 'bacpac をエクスポート'
  $backupDir = Join-Path $RepoRoot '.secrets\sql-backup'
  New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
  $bacpac = Join-Path $backupDir ("{0}-{1:yyyyMMdd-HHmmss}.bacpac" -f $Names.SqlDb, (Get-Date))
  $fqdn = "$($Names.SqlServer).database.windows.net"
  $token = (Invoke-Az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv | Out-String).Trim()
  & sqlpackage /Action:Export /SourceServerName:$fqdn /SourceDatabaseName:$($Names.SqlDb) /AccessToken:$token /TargetFile:$bacpac /SourceEncryptConnection:True
  if ($LASTEXITCODE -ne 0) { throw 'エクスポートに失敗しました。' }
  Write-Done $bacpac

  Write-Step "旧 DB を $oldDb にリネーム"
  $null = Invoke-Az sql db rename -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb --new-name $oldDb -o none
  $renamed = $true

  Write-Step "無料オファー DB $($Names.SqlDb) を作成"
  $null = Invoke-Az sql db create -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb `
    --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless `
    --min-capacity 0.5 --auto-pause-delay 15 --max-size 32GB `
    --backup-storage-redundancy Local --collation Japanese_CI_AS `
    --use-free-limit --free-limit-exhaustion-behavior BillOverUsage -o none
  $created = $true
  Write-Done '作成'

  Write-Step 'bacpac をインポート'
  $token = (Invoke-Az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv | Out-String).Trim()
  & sqlpackage /Action:Import /SourceFile:$bacpac /TargetServerName:$fqdn /TargetDatabaseName:$($Names.SqlDb) /AccessToken:$token /TargetEncryptConnection:True
  if ($LASTEXITCODE -ne 0) { throw 'インポートに失敗しました。' }
  Write-Done 'インポート'

  Write-Step 'DB 権限を付与（API MI / comical-batch-operator）'
  $apiPid = (Invoke-Az functionapp identity show -g $Names.Rg -n $Names.FuncApi --query principalId -o tsv | Out-String).Trim()
  $apiAppId = (Invoke-Az ad sp show --id $apiPid --query appId -o tsv | Out-String).Trim()
  Invoke-SqlAzCli $Names.SqlDb (Get-GrantSql $Names.FuncApi $apiAppId)
  Invoke-SqlAzCli $Names.SqlDb (Get-GrantSql 'comical-batch-operator' (Get-SignedInObjectId))
  Write-Done '権限付与'

  Write-Step 'API を起動してスモークテスト'
  $null = Invoke-Az functionapp start -g $Names.Rg -n $Names.FuncApi -o none
  $apiStopped = $false
  $writesResumed = $true
  $swaHost = (Invoke-Az staticwebapp show -g $Names.Rg -n $Names.Swa --query defaultHostname -o tsv | Out-String).Trim()
  $ok = $false
  for ($i = 1; $i -le 18 -and -not $ok; $i++) {
    try {
      $r = Invoke-WebRequest -Uri "https://$swaHost/api/v1/volumes/upcoming" -UseBasicParsing -TimeoutSec 90
      $ok = ($r.StatusCode -eq 200)
    } catch { Write-Host "  待機中 ($i/18): $($_.Exception.Message)"; Start-Sleep -Seconds 10 }
  }
  if (-not $ok) { Write-Warning 'スモークテストが 200 になりませんでした。手動で確認してください（旧 DB は残っています）。' }
  else { Write-Done "https://$swaHost/api/v1/volumes/upcoming → 200" }

  $new = Invoke-Az sql db show -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb -o json | Out-String | ConvertFrom-Json
  Write-Host ("新 DB: useFreeLimit={0} / exhaustion={1}" -f $new.useFreeLimit, $new.freeLimitExhaustionBehavior)
  Write-Host ''
  Write-Host "数日問題がなければ、旧 DB を削除してください: .\plan\scripts\apply-07-sql-free-offer.ps1 -DeleteOldDatabase" -ForegroundColor Yellow
  Write-Host '（旧 DB は停止中でもストレージ分の課金が続きます）' -ForegroundColor Yellow
}
catch {
  Write-Error $_ -ErrorAction Continue
  if ($renamed -and $writesResumed) {
    Write-Warning "API 再開後に失敗しました。新 DB に書き込みが入っている可能性があるため自動では戻しません。状況を確認し、必要なら手動で $oldDb を正規名に戻してください。"
  }
  elseif ($renamed) {
    Write-Step 'ロールバック: 新 DB を削除し、旧 DB の名前を戻す'
    # create が途中で失敗しても DB が作られている場合があるので、$created ではなく実在で判定する
    $newExists = & az sql db show -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb --query name -o tsv 2>$null
    if ($newExists) {
      & az sql db delete -g $Names.Rg -s $Names.SqlServer -n $Names.SqlDb --yes -o none 2>&1 | Out-Host
      if ($LASTEXITCODE -ne 0) { Write-Warning "新 DB の削除に失敗しました。$oldDb のリネームは手動で行ってください。" }
    }
    & az sql db rename -g $Names.Rg -s $Names.SqlServer -n $oldDb --new-name $Names.SqlDb -o none 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) { Write-Warning "旧 DB の名前を戻せませんでした。手動で実行してください: az sql db rename -g $($Names.Rg) -s $($Names.SqlServer) -n $oldDb --new-name $($Names.SqlDb)" }
  }
  throw
}
finally {
  if ($apiStopped) { & az functionapp start -g $Names.Rg -n $Names.FuncApi -o none 2>&1 | Out-Null }
  if ($rule) { Remove-TempFirewallRule $rule }
  Enable-BatchTask $taskDisabled
  Restore-SqlEntraAdmin $prevAdmin
  Stop-Transcript | Out-Null; Write-Host "ログ: $log"
}
