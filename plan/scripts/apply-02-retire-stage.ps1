<#
.SYNOPSIS
  Phase 1: Stage(dev) 環境の廃止。cmcl-dev-jpe-rg を丸ごと削除する（元に戻せない）。

  - リソースグループ cmcl-dev-jpe-rg の削除（SQL / NAT / PIP / VNet / Functions / SWA(Free) / App Config(Free) / KV など）
  - ソフト削除された Key Vault / App Configuration の purge（名前と Free 枠を解放）
  - （任意）GitHub Environment "dev" の削除（gh CLI が必要）
  - （任意）OIDC フェデレーション資格情報のうち environment:dev のものを削除（-AzureClientId 指定時）

  コード側（cd-dev.yml / dev-sql-nightly-pause.yml / pr-preview.yml / dev.bicepparam）は同じブランチで削除済み。
#>
[CmdletBinding()]
param([switch]$Yes, [string]$AzureClientId = '')
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-02'
try {
  Assert-AzLogin

  $exists = (Invoke-Az group exists -n $Names.DevRg | Out-String).Trim()
  if ($exists -eq 'true') {
    Write-Step "$($Names.DevRg) の中身"
    Invoke-Az resource list -g $Names.DevRg --query "[].{type:type,name:name}" -o table | Out-Host
    if (-not (Confirm-Action "$($Names.DevRg) を削除します。データ（dev の SQL を含む）は復元できません。続行しますか？")) { throw '中止しました。' }
    Write-Step 'リソースグループを削除（数分かかります）'
    $null = Invoke-Az group delete -n $Names.DevRg --yes -o none
    Write-Done "$($Names.DevRg) を削除"
  } else { Write-Skip "$($Names.DevRg) は既にありません" }

  Write-Step 'ソフト削除済み Key Vault / App Configuration を purge'
  $kv = & az keyvault list-deleted --query "[?name=='$($Names.DevKv)'].name | [0]" -o tsv 2>$null
  if ($kv) { $null = Invoke-Az keyvault purge -n $Names.DevKv -o none; Write-Done "purge: $($Names.DevKv)" } else { Write-Skip 'dev Key Vault の削除済みエントリなし' }
  $ac = & az appconfig list-deleted --query "[?name=='$($Names.DevAppCfg)'].name | [0]" -o tsv 2>$null
  if ($ac) { $null = Invoke-Az appconfig purge -n $Names.DevAppCfg --location $Names.Location --yes -o none; Write-Done "purge: $($Names.DevAppCfg)" } else { Write-Skip 'dev App Configuration の削除済みエントリなし' }

  if (Get-Command gh -ErrorAction SilentlyContinue) {
    if (Confirm-Action "GitHub Environment 'dev'（$($Names.GitHubRepo)）を削除しますか？（dev 用のシークレットも消えます）") {
      & gh api -X DELETE "repos/$($Names.GitHubRepo)/environments/dev" 2>&1 | Out-Host
      Write-Done 'GitHub Environment dev を削除'
    }
  } else { Write-Skip 'gh CLI がないため GitHub Environment dev は手動で削除してください（Settings → Environments）' }

  if ($AzureClientId) {
    Write-Step "Entra アプリ $AzureClientId の federated credential（environment:dev）"
    $fcs = Invoke-Az ad app federated-credential list --id $AzureClientId -o json | Out-String | ConvertFrom-Json
    foreach ($fc in $fcs | Where-Object { $_.subject -like '*:environment:dev' }) {
      if (Confirm-Action "federated credential '$($fc.name)'（$($fc.subject)）を削除しますか？") {
        $null = Invoke-Az ad app federated-credential delete --id $AzureClientId --federated-credential-id $fc.id -o none
        Write-Done "削除: $($fc.name)"
      }
    }
  } else { Write-Skip 'federated credential は -AzureClientId 指定時のみ整理します' }
}
finally { Stop-Transcript | Out-Null; Write-Host "ログ: $log" }
