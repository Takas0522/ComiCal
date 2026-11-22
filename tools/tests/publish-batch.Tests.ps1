BeforeAll {
  $publish = Join-Path $PSScriptRoot '../publish-batch.ps1'
  . $publish
}

Describe 'Batch releases' {
  BeforeEach {
    $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
    $releasesPath = Join-Path $root 'releases'
    $newest = '20261122-030000-abcdef0'
    $middle = '20261121-030000-abcdef1'
    $oldest = '20261120-030000-abcdef2'
    foreach ($name in @($oldest, $newest, $middle, '.publish-partial')) {
      New-Item -ItemType Directory -Path (Join-Path $releasesPath $name) -Force | Out-Null
    }
  }

  It 'generates and sorts release names independently of staging directories' {
    Format-BatchReleaseName ([datetime]::new(2026, 11, 22, 3, 0, 0)) 'abcdef0' | Should -Be $newest
    @(Get-BatchReleaseName $releasesPath) | Should -Be @($newest, $middle, $oldest)
  }

  It 'selects the previous release relative to current and rejects the oldest' {
    $releases = @(Get-BatchReleaseName $releasesPath)
    Get-BatchRollbackRelease $releases $middle | Should -Be $oldest
    { Get-BatchRollbackRelease $releases $oldest } | Should -Throw '*No preceding release*'
    { Get-BatchRollbackRelease $releases $null } | Should -Throw '*No preceding release*'
  }

  It 'keeps current within the retention limit even after a rollback' {
    $releases = @(Get-BatchReleaseName $releasesPath)
    Get-BatchExpiredRelease $releases $newest 2 | Should -Be $oldest
    Get-BatchExpiredRelease $releases $oldest 2 | Should -Be $middle
    @(Get-BatchExpiredRelease $releases $oldest 1) | Should -Be @($newest, $middle)
  }

  It 'rolls back a symbolic link without deleting the previous target' -Skip:($env:OS -eq 'Windows_NT') {
    $currentPath = Join-Path $root 'current'
    Set-BatchCurrentLink $currentPath (Join-Path $releasesPath $newest) $false 'test-task'
    & $publish -Root $root -Rollback
    Get-BatchCurrentRelease $currentPath $releasesPath @(Get-BatchReleaseName $releasesPath) | Should -Be $middle
    Test-Path -LiteralPath (Join-Path $releasesPath $newest) -PathType Container | Should -BeTrue
    @(Get-ChildItem -LiteralPath $root | Where-Object Name -match '^current\.').Count | Should -Be 0
  }

  It 'keeps current when Batch starts before the link swap' -Skip:($env:OS -eq 'Windows_NT') {
    $currentPath = Join-Path $root 'current'
    New-Item -ItemType SymbolicLink -Path $currentPath -Target (Join-Path $releasesPath $newest) | Out-Null
    Mock Assert-BatchIdle { throw 'ComiCal.Batch is running.' }
    {
      Set-BatchCurrentLink $currentPath (Join-Path $releasesPath $middle) $false 'test-task'
    } | Should -Throw '*ComiCal.Batch is running*'
    Get-BatchCurrentRelease $currentPath $releasesPath @(Get-BatchReleaseName $releasesPath) | Should -Be $newest
    @(Get-ChildItem -LiteralPath $root | Where-Object Name -match '^current\.').Count | Should -Be 0
  }
}
