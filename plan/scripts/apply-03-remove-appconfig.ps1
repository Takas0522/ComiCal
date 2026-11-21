<#
.SYNOPSIS
  Phase 2: 未使用の App Configuration (prod, Standard) を削除する。
  コードは App Configuration を一切参照していない（SDK 未導入、Feature Flag 3 件はすべて false）。
  app.bicep からも appConfig と AppConfiguration__Endpoint を削除済み。
#>
[CmdletBinding()]
param([switch]$Yes)
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-03'
try {
  Assert-AzLogin

  foreach ($app in @($Names.FuncApi, $Names.FuncBatch)) {
    if (Test-AzResource 'Microsoft.Web/sites' $Names.Rg $app) {
      Write-Step "$app から AppConfiguration__Endpoint を削除"
      $null = Invoke-Az functionapp config appsettings delete -g $Names.Rg -n $app --setting-names AppConfiguration__Endpoint -o none
      Write-Done $app
    }
  }

  if (Test-AzResource 'Microsoft.AppConfiguration/configurationStores' $Names.Rg $Names.AppCfg) {
    if (-not (Confirm-Action "$($Names.AppCfg)（Feature Flag 3 件, すべて無効）を削除しますか？")) { throw '中止しました。' }
    $null = Invoke-Az appconfig delete -g $Names.Rg -n $Names.AppCfg --yes -o none
    Write-Done "削除: $($Names.AppCfg)"
  } else { Write-Skip "$($Names.AppCfg) は既にありません" }

  $ac = & az appconfig list-deleted --query "[?name=='$($Names.AppCfg)'].name | [0]" -o tsv 2>$null
  if ($ac) {
    # ソフト削除中も課金はされないが、名前を解放しておく
    $null = Invoke-Az appconfig purge -n $Names.AppCfg --location $Names.Location --yes -o none
    Write-Done "purge: $($Names.AppCfg)"
  }
}
finally { Stop-Transcript | Out-Null; Write-Host "ログ: $log" }
