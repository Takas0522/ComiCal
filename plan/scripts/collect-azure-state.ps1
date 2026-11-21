<#
  ComiCal コスト削減計画用: 現在の Azure 状態を読み取り専用で収集するスクリプト
  - 変更系コマンドは一切実行しません（show / list / query / metrics のみ）
  - 出力先: .secrets/azure-snapshot/ （.gitignore 済み）
  使い方: リポジトリ直下で  pwsh -File .\plan\scripts\collect-azure-state.ps1   （Windows PowerShell 5.1 でも可）
#>
$ErrorActionPreference = 'Continue'
$out = Join-Path $PSScriptRoot '..\..\.secrets\azure-snapshot'
New-Item -ItemType Directory -Force -Path $out | Out-Null
function Save($name, [scriptblock]$cmd) {
  Write-Host "-> $name"
  try { & $cmd 2>&1 | Out-File -Encoding utf8 (Join-Path $out $name) } catch { $_ | Out-File -Encoding utf8 (Join-Path $out $name) }
}

$sub = az account show --query id -o tsv
Save 'account.json'        { az account show -o json }
Save 'groups.json'         { az group list -o json }
Save 'resources.json'      { az resource list -o json }
Save 'sql-servers.json'    { az sql server list -o json }
Save 'functionapps.json'   { az functionapp list -o json }
Save 'staticwebapps.json'  { az staticwebapp list -o json }
Save 'appconfig.json'      { az appconfig list -o json }
Save 'nat.json'            { az network nat gateway list -o json }
Save 'publicip.json'       { az network public-ip list -o json }
Save 'vnet.json'           { az network vnet list -o json }
Save 'law.json'            { az monitor log-analytics workspace list -o json }
Save 'appinsights.json'    { az monitor app-insights component show -o json }
Save 'alerts.json'         { az resource list --resource-type Microsoft.Insights/scheduledQueryRules -o json }
Save 'metric-alerts.json'  { az monitor metrics alert list -o json }
Save 'actiongroups.json'   { az monitor action-group list -o json }
Save 'keyvaults.json'      { az keyvault list -o json }
Save 'storage.json'        { az storage account list -o json }
Save 'budgets.json'        { az rest --method get --url "https://management.azure.com/subscriptions/$sub/providers/Microsoft.Consumption/budgets?api-version=2023-05-01" }
Save 'advisor-cost.json'   { az advisor recommendation list --category Cost -o json }

# SQL DB 詳細 + 30日間の課金 vCore 秒
$servers = az sql server list --query "[].{n:name,g:resourceGroup}" -o json | ConvertFrom-Json
foreach ($s in $servers) {
  Save "sqldb-$($s.n).json" { az sql db list -g $s.g -s $s.n -o json }
  $dbs = az sql db list -g $s.g -s $s.n --query "[?name!='master'].id" -o tsv
  foreach ($id in $dbs) {
    $leaf = ($id -split '/')[-1]
    Save "sqlmetrics-$leaf.json" { az monitor metrics list --resource $id --metric app_cpu_billed cpu_percent allocated_data_storage --interval P1D --aggregation Total Maximum --offset 30d -o json }
  }
}

# Log Analytics 取り込み量（30日, DataType別）
$laws = az monitor log-analytics workspace list --query "[].{n:name,c:customerId}" -o json | ConvertFrom-Json
foreach ($w in $laws) {
  Save "law-usage-$($w.n).json" { az monitor log-analytics query -w $w.c --analytics-query "Usage | where TimeGenerated > ago(30d) | where IsBillable == true | summarize GB=sum(Quantity)/1024 by DataType | order by GB desc" -o json }
}

# Cost Management: 直近3か月 (月次) をリソース×メーター / RG×サービスで集計
$from = (Get-Date).AddMonths(-3).ToString('yyyy-MM-01')
$to   = (Get-Date).ToString('yyyy-MM-dd')
$scope = "https://management.azure.com/subscriptions/$sub/providers/Microsoft.CostManagement/query?api-version=2023-11-01"
$q = @(
  @{ file='cost-by-resource-meter.json'; g=@(@{type='Dimension';name='ResourceId'}, @{type='Dimension';name='Meter'}) },
  @{ file='cost-by-rg-service.json';     g=@(@{type='Dimension';name='ResourceGroupName'}, @{type='Dimension';name='ServiceName'}) }
)
foreach ($item in $q) {
  $body = @{ type='ActualCost'; timeframe='Custom'; timePeriod=@{from=$from;to=$to};
    dataset=@{ granularity='Monthly'; aggregation=@{ totalCost=@{name='Cost';function='Sum'} }; grouping=$item.g } } | ConvertTo-Json -Depth 10
  $tmp = Join-Path $out 'body.tmp.json'
  [IO.File]::WriteAllText($tmp, $body)
  Save $item.file { az rest --method post --url $scope --body "@$tmp" }
  Start-Sleep -Seconds 5   # Cost Management API のスロットリング回避
}
Remove-Item (Join-Path $out 'body.tmp.json') -ErrorAction SilentlyContinue
Write-Host "完了: $out"
