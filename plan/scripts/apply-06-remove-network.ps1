<#
.SYNOPSIS
  Phase 4（D-1）: API の楽天連携を無効化し、NAT Gateway / Public IP / VNet を撤去する。

  - API Function App から楽天 3 設定と AppConfiguration__Endpoint を削除
    → RakutenApplicationId 未設定のため NullRakutenBookSearchService が使われ、検索は DB のみ・楽天候補は空
  - API の VNet 統合を解除し、vnetRouteAllEnabled を false に
  - VNet → NAT Gateway → Public IP の順に削除（関連付けの都合でこの順）
  app.bicep / main.bicep からは同じ内容を削除済み（modules/network.bicep も削除）。
#>
[CmdletBinding()]
param([switch]$Yes)
. (Join-Path $PSScriptRoot '_common.ps1')
$Script:AssumeYes = [bool]$Yes
$log = Start-ApplyLog 'apply-06'
try {
  Assert-AzLogin
  if (Test-AzResource 'Microsoft.Web/sites' $Names.Rg $Names.FuncBatch) {
    throw "$($Names.FuncBatch) がまだ存在します（同じサブネットを使用中）。先に apply-05 を実行してください。"
  }
  if (-not (Confirm-Action 'API の楽天連携（検索フォールバック / 楽天候補からの購読）を無効化し、NAT Gateway を削除します。続行しますか？')) { throw '中止しました。' }

  Write-Step "$($Names.FuncApi): 楽天設定を削除"
  $null = Invoke-Az functionapp config appsettings delete -g $Names.Rg -n $Names.FuncApi `
    --setting-names RakutenApplicationId RakutenAccessKey RakutenAffiliateId AppConfiguration__Endpoint -o none
  Write-Done 'app settings'

  Write-Step "$($Names.FuncApi): VNet 統合を解除"
  $subnet = (& az functionapp show -g $Names.Rg -n $Names.FuncApi --query virtualNetworkSubnetId -o tsv 2>$null | Out-String).Trim()
  if ($subnet) {
    $null = Invoke-Az functionapp vnet-integration remove -g $Names.Rg -n $Names.FuncApi -o none
    Write-Done "解除: $subnet"
  } else { Write-Skip 'VNet 統合なし' }
  & az functionapp config set -g $Names.Rg -n $Names.FuncApi --vnet-route-all-enabled false -o none 2>&1 | Out-Null
  if ($LASTEXITCODE -ne 0) { Write-Warning 'vnetRouteAllEnabled の更新に失敗（VNet 統合解除済みなら影響なし）' }

  Write-Step 'スモークテスト（SWA 経由）'
  $swaHost = (Invoke-Az staticwebapp show -g $Names.Rg -n $Names.Swa --query defaultHostname -o tsv | Out-String).Trim()
  Start-Sleep -Seconds 20
  $ok = $false
  for ($i = 1; $i -le 12 -and -not $ok; $i++) {
    try {
      $r = Invoke-WebRequest -Uri "https://$swaHost/api/v1/volumes/upcoming" -UseBasicParsing -TimeoutSec 90
      $ok = ($r.StatusCode -eq 200)
    } catch { Write-Host "  待機中 ($i/12): $($_.Exception.Message)"; Start-Sleep -Seconds 10 }
  }
  if (-not $ok) { throw 'API のスモークテストに失敗しました。ネットワークリソースは削除していません。' }
  Write-Done "https://$swaHost/api/v1/volumes/upcoming → 200"

  Write-Step 'VNet → NAT Gateway → Public IP を削除'
  if (Test-AzResource 'Microsoft.Network/virtualNetworks' $Names.Rg $Names.Vnet) {
    $null = Invoke-Az network vnet delete -g $Names.Rg -n $Names.Vnet -o none; Write-Done $Names.Vnet
  }
  if (Test-AzResource 'Microsoft.Network/natGateways' $Names.Rg $Names.Nat) {
    $null = Invoke-Az network nat gateway delete -g $Names.Rg -n $Names.Nat -o none; Write-Done $Names.Nat
  }
  if (Test-AzResource 'Microsoft.Network/publicIPAddresses' $Names.Rg $Names.Pip) {
    $pipAddr = (Invoke-Az network public-ip show -g $Names.Rg -n $Names.Pip --query ipAddress -o tsv | Out-String).Trim()
    $null = Invoke-Az network public-ip delete -g $Names.Rg -n $Names.Pip -o none; Write-Done "$($Names.Pip) ($pipAddr)"
    Write-Host ''
    Write-Host "★ 楽天ウェブサービスのアプリ設定から旧 Azure の許可 IP $pipAddr を削除してください。" -ForegroundColor Yellow
  }
}
finally { Stop-Transcript | Out-Null; Write-Host "ログ: $log" }
