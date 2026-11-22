<#
.SYNOPSIS
  Publish ComiCal.Batch for win-x64, switch current, and retain recent releases.
.DESCRIPTION
  Use -List to inspect releases or -Rollback to switch to the preceding release.
  Use the same -TaskName as register-batch-task.ps1 if you changed the default.
  -Root only relocates the binaries (releases/current), e.g. for testing. ComiCal.Batch always reads
  appsettings.Local.json and writes logs under its fixed data folder (BatchPaths.Root), so the settings
  file is created there regardless of -Root.
#>
[CmdletBinding(DefaultParameterSetName = 'Publish')]
param(
  [string]$Root,
  [string]$TaskName = 'ComiCal-DailyBatch',
  [Parameter(ParameterSetName = 'Publish')][ValidateRange(1, 2147483647)][int]$Keep = 3,
  [Parameter(ParameterSetName = 'Publish')][switch]$SkipTests,
  [Parameter(ParameterSetName = 'Publish')][switch]$AllowDirty,
  [Parameter(Mandatory, ParameterSetName = 'Rollback')][switch]$Rollback,
  [Parameter(Mandatory, ParameterSetName = 'List')][switch]$List
)

function Format-BatchReleaseName([datetime]$Timestamp, [string]$Sha) {
  '{0}-{1}' -f $Timestamp.ToString('yyyyMMdd-HHmmss', [Globalization.CultureInfo]::InvariantCulture), $Sha
}

function Get-BatchReleaseName([string]$ReleasesPath) {
  if (Test-Path -LiteralPath $ReleasesPath -PathType Container) {
    Get-ChildItem -LiteralPath $ReleasesPath -Directory | Where-Object {
      $_.Name -cmatch '^\d{8}-\d{6}-[a-f0-9]+$' -and
      -not ($_.Attributes -band [IO.FileAttributes]::ReparsePoint)
    } | Sort-Object Name -Descending | Select-Object -ExpandProperty Name
  }
}

function Get-BatchRollbackRelease([string[]]$Releases, [string]$Current) {
  $index = [array]::IndexOf($Releases, $Current)
  if ($index -lt 0 -or $index + 1 -ge $Releases.Count) {
    throw 'No preceding release exists for current.'
  }
  $Releases[$index + 1]
}

function Get-BatchExpiredRelease([string[]]$Releases, [string]$Current, [int]$Count) {
  # A rolled-back current occupies one of the retention slots.
  $Releases | Where-Object { $_ -ne $Current } | Select-Object -Skip ($Count - 1)
}

function Get-BatchCurrentRelease([string]$CurrentPath, [string]$ReleasesPath, [string[]]$Releases) {
  $item = Get-Item -LiteralPath $CurrentPath -Force -ErrorAction SilentlyContinue
  if (-not $item) { return }
  if ($item.LinkType -notin 'Junction', 'SymbolicLink') { throw "$CurrentPath must be a release link." }
  $target = [string](@($item.Target)[0])
  if ($target -notin @($Releases | ForEach-Object { Join-Path $ReleasesPath $_ })) {
    throw "$CurrentPath points outside the available releases or has a missing target: $target"
  }
  Split-Path -Leaf $target
}

function Assert-BatchIdle([string]$Name, [bool]$OnWindows) {
  if ($OnWindows -and (Get-ScheduledTask -ErrorAction Stop | Where-Object {
    $_.TaskName -eq $Name -and $_.State -eq 'Running'
  })) { throw "Scheduled task $Name is running." }
  if (Get-Process -Name 'ComiCal.Batch' -ErrorAction SilentlyContinue) {
    throw 'ComiCal.Batch is running.'
  }
}

function Set-BatchCurrentLink {
  [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
    Justification = 'Internal step of publish or rollback, which also changes release directories.')]
  param([string]$CurrentPath, [string]$Target, [bool]$OnWindows, [string]$Name)
  $suffix = [guid]::NewGuid().ToString('N')
  $pending = "$CurrentPath.new-$suffix"
  $backup = "$CurrentPath.old-$suffix"
  $linkType = if ($OnWindows) { 'Junction' } else { 'SymbolicLink' }
  New-Item -ItemType $linkType -Path $pending -Target $Target | Out-Null
  try {
    Assert-BatchIdle $Name $OnWindows
    if (Test-Path -LiteralPath $CurrentPath) { [IO.Directory]::Move($CurrentPath, $backup) }
    try {
      [IO.Directory]::Move($pending, $CurrentPath)
    } catch {
      if (Test-Path -LiteralPath $backup) { [IO.Directory]::Move($backup, $CurrentPath) }
      throw
    }
    if (Test-Path -LiteralPath $backup) {
      # Nonrecursive deletion removes only the link, including on Windows PowerShell 5.1.
      try { [IO.Directory]::Delete($backup) }
      catch { Write-Warning "Could not remove old link $backup. $_" }
    }
  } finally {
    if (Test-Path -LiteralPath $pending) { [IO.Directory]::Delete($pending) }
  }
}

# Dot-sourcing exposes the helpers to the tests.
if ($MyInvocation.InvocationName -eq '.') { return }

$ErrorActionPreference = 'Stop'
$onWindows = $env:OS -eq 'Windows_NT'
# Must match src/backend/batch/ComiCal.Batch/Hosting/BatchPaths.cs.
$data = if ($onWindows) { [Environment]::GetFolderPath('LocalApplicationData') } else { Join-Path $HOME '.local/share' }
$dataRoot = Join-Path $data 'ComiCal/Batch'
if (-not $Root) { $Root = $dataRoot }
$Root = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Root)
$releasesPath = Join-Path $Root 'releases'
$currentPath = Join-Path $Root 'current'
if ($List) {
  $releases = @(Get-BatchReleaseName $releasesPath)
  $current = Get-BatchCurrentRelease $currentPath $releasesPath $releases
  Write-Information -InformationAction Continue "current: $current"
  $releases
  return
}

Assert-BatchIdle $TaskName $onWindows
New-Item -ItemType Directory -Path $releasesPath -Force | Out-Null
# Serialize publish and rollback for this installation.
$lock = [IO.File]::Open((Join-Path $Root '.publish.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
try {
  $releases = @(Get-BatchReleaseName $releasesPath)
  $current = Get-BatchCurrentRelease $currentPath $releasesPath $releases
  if ($Rollback) {
    $previous = Get-BatchRollbackRelease $releases $current
    Set-BatchCurrentLink $currentPath (Join-Path $releasesPath $previous) $onWindows $TaskName
    Write-Information -InformationAction Continue "current: $previous"
    return
  }

  $repo = Split-Path -Parent $PSScriptRoot
  $project = Join-Path $repo 'src/backend/batch/ComiCal.Batch'
  $testProject = Join-Path $repo 'src/tests/backend/ComiCal.Batch.Tests'
  $dirty = & git -C $repo status --porcelain
  if ($LASTEXITCODE -ne 0) { throw 'git status failed.' }
  if ($dirty) {
    Write-Warning 'The working tree contains uncommitted changes.'
    if (-not $AllowDirty) { throw 'Use -AllowDirty to publish the working tree explicitly.' }
  }
  $sha = & git -C $repo rev-parse --short HEAD
  if ($LASTEXITCODE -ne 0) { throw 'git rev-parse failed.' }

  Push-Location $repo
  try {
    if (-not $SkipTests) {
      & dotnet test $testProject -p:RestoreLockedMode=true
      if ($LASTEXITCODE -ne 0) { throw "dotnet test failed ($LASTEXITCODE)." }
    }
    $release = Format-BatchReleaseName (Get-Date) $sha
    $destination = Join-Path $releasesPath $release
    if (Test-Path -LiteralPath $destination) { throw "Release already exists: $destination" }
    $staging = Join-Path $releasesPath ('.publish-' + [guid]::NewGuid().ToString('N'))
    try {
      # The RID restore must not regenerate checked-in lock files.
      & dotnet publish $project -c Release -r win-x64 --self-contained false `
        -p:RestorePackagesWithLockFile=false -p:NuGetLockFilePath=obj/batch-publish.lock.json -o $staging
      if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

      $settings = Join-Path $dataRoot 'appsettings.Local.json'
      if (-not (Test-Path -LiteralPath $settings)) {
        New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
        [IO.File]::Copy((Join-Path $project 'appsettings.Local.json.sample'), $settings)
        Write-Warning "Review and edit $settings before running Batch."
      }

      [IO.Directory]::Move($staging, $destination)
      try {
        Set-BatchCurrentLink $currentPath $destination $onWindows $TaskName
      } catch {
        Remove-Item -LiteralPath $destination -Recurse -Force
        throw
      }
    } finally {
      if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    }
  } finally {
    Pop-Location
  }

  $releases = @(Get-BatchReleaseName $releasesPath)
  foreach ($expired in (Get-BatchExpiredRelease $releases $release $Keep)) {
    Remove-Item -LiteralPath (Join-Path $releasesPath $expired) -Recurse -Force
  }
  Write-Information -InformationAction Continue "current: $release"
} finally {
  $lock.Dispose()
}
