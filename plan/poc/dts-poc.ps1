#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9-]*poc[A-Za-z0-9-]*-rg$')]
    [string] $ResourceGroup = 'cmcl-poc-dts-rg',

    [ValidatePattern('^[a-zA-Z0-9]+$')]
    [string] $Location = 'japaneast',

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-zA-Z0-9-]{3,64}$')]
    [string] $SchedulerName,

    [ValidatePattern('^[a-zA-Z0-9-]{3,64}$')]
    [string] $TaskHub = 'poc',

    [Parameter(Mandatory)]
    [ValidateSet('Create', 'SetAllowlist', 'Run', 'Cleanup')]
    [string] $Action,

    [string] $AllowIp,

    [string[]] $AppArguments = @('run')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-AzCli {
    & az @args
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed ($LASTEXITCODE): $($args[0..1] -join ' ')"
    }
}

function Invoke-ArmWrite {
    param([string] $Method, [string] $Uri, [hashtable] $Body)

    # A file preserves JSON quotes when PowerShell on Windows invokes az.cmd.
    $bodyFile = New-TemporaryFile
    try {
        $Body | ConvertTo-Json -Depth 6 -Compress |
            Set-Content -LiteralPath $bodyFile.FullName -Encoding utf8NoBOM
        Invoke-AzCli rest --method $Method --url $Uri --body "@$($bodyFile.FullName)" --headers 'Content-Type=application/json' --output none
    }
    finally {
        Remove-Item -LiteralPath $bodyFile.FullName
    }
}

function Wait-DtsResource {
    param([string] $Uri, [string] $ExpectedIp)

    $deadline = [DateTimeOffset]::UtcNow.AddMinutes(15)
    do {
        $resource = Invoke-AzCli rest --method get --url $Uri --output json | ConvertFrom-Json -AsHashtable
        switch ($resource.properties.provisioningState) {
            'Succeeded' {
                if (-not $ExpectedIp) {
                    return $resource
                }
                $ips = $resource.properties['ipAllowlist']
                if ($ips -and $ips.Count -eq 1 -and $ips[0] -eq $ExpectedIp) {
                    return $resource
                }
            }
            'Failed' { throw "Provisioning failed: $Uri" }
            'Canceled' { throw "Provisioning canceled: $Uri" }
        }
        Start-Sleep -Seconds 5
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "Timed out waiting for provisioning and the requested allowlist: $Uri"
}

if ($ResourceGroup -match 'prod') {
    throw 'Use a dedicated PoC resource group. Production resource groups are forbidden.'
}
if ($Action -eq 'SetAllowlist' -and [string]::IsNullOrWhiteSpace($AllowIp)) {
    throw '-AllowIp is required for SetAllowlist.'
}

$account = Invoke-AzCli account show --output json | ConvertFrom-Json -AsHashtable
$subscriptionId = $account.id
if ($account.environmentName -ne 'AzureCloud') {
    throw 'Select an Azure public cloud subscription.'
}

$groupArgs = @('--name', $ResourceGroup, '--subscription', $subscriptionId)
$groupExists = (Invoke-AzCli group exists @groupArgs --output tsv) -eq 'true'
if ($groupExists) {
    $group = Invoke-AzCli group show @groupArgs --output json | ConvertFrom-Json -AsHashtable
    $tags = $group['tags']
    if (-not $tags -or $tags['comical-purpose'] -ne 'dts-poc') {
        throw "Existing group '$ResourceGroup' is not tagged comical-purpose=dts-poc."
    }
}
elseif ($Action -ne 'Create') {
    throw "PoC group '$ResourceGroup' does not exist. Run Create first."
}

if ($Action -eq 'Cleanup') {
    $answer = Read-Host "Delete group '$ResourceGroup' and every resource in subscription '$subscriptionId'? Type the group name"
    if ($answer -cne $ResourceGroup) {
        Write-Host 'Cleanup canceled.'
        return
    }
    Invoke-AzCli group delete @groupArgs --yes --output none
    Write-Host "Deleted $ResourceGroup."
    return
}

# Stable ARM API with Consumption, ipAllowlist, and task hub CRUD.
# https://learn.microsoft.com/en-us/azure/templates/microsoft.durabletask/2026-02-01/schedulers
# https://learn.microsoft.com/en-us/rest/api/durabletask/schedulers/create-or-update?view=rest-durabletask-2026-02-01
# https://learn.microsoft.com/en-us/rest/api/durabletask/schedulers/update?view=rest-durabletask-2026-02-01
# https://learn.microsoft.com/en-us/rest/api/durabletask/task-hubs/create-or-update?view=rest-durabletask-2026-02-01
$apiVersion = '2026-02-01'
$schedulerId = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.DurableTask/schedulers/$SchedulerName"
$armBase = 'https://management.azure.com'
$schedulerUri = "${armBase}${schedulerId}?api-version=$apiVersion"
$hubUri = "${armBase}${schedulerId}/taskHubs/${TaskHub}?api-version=$apiVersion"

if ($Action -in 'Create', 'SetAllowlist') {
    if ([string]::IsNullOrWhiteSpace($AllowIp)) {
        $AllowIp = (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).Trim()
    }
    $parsedIp = $null
    if ($AllowIp -notmatch '^(?:\d{1,3}\.){3}\d{1,3}$' -or
        -not [System.Net.IPAddress]::TryParse($AllowIp, [ref] $parsedIp) -or
        $parsedIp.ToString() -ne $AllowIp -or $AllowIp -eq '0.0.0.0') {
        throw 'Provide a single IPv4 address in dotted decimal format.'
    }
}

if ($Action -eq 'Create') {
    $extensions = Invoke-AzCli extension list --query "[?name=='durabletask'].name" --output tsv
    if (-not $extensions) {
        Write-Host 'Azure CLI can auto-install durabletask on first use. To install explicitly, run: az extension add --name durabletask'
    }

    if ($account.user.type -ne 'user') {
        throw 'Sign in to Azure CLI as the user who will run the PoC.'
    }
    $principalId = Invoke-AzCli ad signed-in-user show --query id --output tsv
    $registration = Invoke-AzCli provider show --namespace Microsoft.DurableTask --subscription $subscriptionId --query registrationState --output tsv
    if ($registration -ne 'Registered') {
        Invoke-AzCli provider register --namespace Microsoft.DurableTask --subscription $subscriptionId --wait --output none
    }

    if (-not $groupExists) {
        Invoke-AzCli group create @groupArgs --location $Location --tags 'comical-purpose=dts-poc' --output none
    }

    Invoke-ArmWrite -Method put -Uri $schedulerUri -Body @{
        location = $Location
        properties = @{
            sku = @{ name = 'Consumption' }
            publicNetworkAccess = 'Enabled'
            ipAllowlist = @($AllowIp)
        }
    }
    $scheduler = Wait-DtsResource -Uri $schedulerUri -ExpectedIp $AllowIp

    Invoke-ArmWrite -Method put -Uri $hubUri -Body @{ properties = @{} }
    $hub = Wait-DtsResource -Uri $hubUri
    $roleName = 'Durable Task Data Contributor'
    $assignments = Invoke-AzCli role assignment list --scope $schedulerId --subscription $subscriptionId --query "[?scope=='$schedulerId' && principalId=='$principalId' && roleDefinitionName=='$roleName'].id" --output tsv
    if (-not $assignments) {
        Invoke-AzCli role assignment create --assignee-object-id $principalId --assignee-principal-type User --role $roleName --scope $schedulerId --subscription $subscriptionId --output none
    }

    Write-Host "Location=$($scheduler.location) SKU=$($scheduler.properties.sku.name) TaskHub=$TaskHub"
    Write-Host "Endpoint=$($scheduler.properties.endpoint)"
    Write-Host "Dashboard=$($hub.properties.dashboardUrl)"
}
elseif ($Action -eq 'SetAllowlist') {
    Invoke-ArmWrite -Method patch -Uri $schedulerUri -Body @{ properties = @{ ipAllowlist = @($AllowIp) } }
    $null = Wait-DtsResource -Uri $schedulerUri -ExpectedIp $AllowIp
}
else {
    $scheduler = Wait-DtsResource -Uri $schedulerUri
    $previousConnection = $env:DTS_CONNECTION_STRING
    $previousCredential = $env:AZURE_TOKEN_CREDENTIALS
    try {
        $env:DTS_CONNECTION_STRING = "Endpoint=$($scheduler.properties.endpoint);TaskHub=$TaskHub;Authentication=DefaultAzure"
        $env:AZURE_TOKEN_CREDENTIALS = 'AzureCliCredential'
        & dotnet run --project (Join-Path $PSScriptRoot 'DtsPocApp') -- @AppArguments
        $appExitCode = $LASTEXITCODE
    }
    finally {
        $env:DTS_CONNECTION_STRING = $previousConnection
        $env:AZURE_TOKEN_CREDENTIALS = $previousCredential
    }
    exit $appExitCode
}

Write-Host "ipAllowlist=$AllowIp"
