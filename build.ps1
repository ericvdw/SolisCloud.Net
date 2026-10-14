<#
.SYNOPSIS
    Restores, builds and tests the solution. Every step's output is also written to artifacts/logs.

.DESCRIPTION
    Warnings are errors (see Directory.Build.props), so a clean run means no analyzer findings anywhere.
    artifacts/logs/summary.txt lists each step's result; build.binlog can be opened with the MSBuild
    Structured Log Viewer (https://msbuildlog.com) to see exactly why a project built the way it did.

.PARAMETER Configuration
    Debug or Release. Defaults to Release.

.PARAMETER SkipTests
    Restore and build only.

.PARAMETER UpdatePublicApi
    First add any new public API members to src/*/PublicAPI.Unshipped.txt (the RS0016 code fix), then build.
    Review the diff afterwards: every line is a public API commitment.

.EXAMPLE
    ./build.ps1

.EXAMPLE
    ./build.ps1 -UpdatePublicApi
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipTests,

    [switch] $UpdatePublicApi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$solution = Join-Path $root 'SolisCloud.slnx'
$logs = Join-Path $root 'artifacts/logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
Get-ChildItem -Path $logs -File | Remove-Item

$summary = [System.Collections.Generic.List[string]]::new()

function Write-Summary {
    $summary | Set-Content -Path (Join-Path $logs 'summary.txt') -Encoding utf8
    Write-Host ''
    $summary | ForEach-Object { Write-Host $_ }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    $log = Join-Path $logs "$Name.log"
    Write-Host "==> dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    # Windows PowerShell turns native stderr into error records; don't let that abort the script.
    $ErrorActionPreference = 'Continue'
    & dotnet @Arguments 2>&1 |
        ForEach-Object { $line = "$_"; Write-Host $line; $line } |
        Set-Content -Path $log -Encoding utf8
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'

    $status = if ($exitCode -eq 0) { 'OK' } else { "FAILED (exit code $exitCode)" }
    $summary.Add(('{0,-10} {1,-26} {2,6:n1}s  {3}' -f $Name, $status, $stopwatch.Elapsed.TotalSeconds, $log))
    if ($exitCode -ne 0) {
        Write-Summary
        throw "Step '$Name' failed. See $log"
    }
}

dotnet --info *> (Join-Path $logs 'dotnet-info.log')

if ($UpdatePublicApi) {
    Invoke-Step -Name 'publicapi' -Arguments @('format', 'analyzers', $solution, '--diagnostics', 'RS0016', 'RS0017', '--severity', 'info', '--verbosity', 'normal')
}

Invoke-Step -Name 'restore' -Arguments @('restore', $solution)
Invoke-Step -Name 'build' -Arguments @('build', $solution, '--no-restore', '--configuration', $Configuration, "-bl:$(Join-Path $logs 'build.binlog')")

if (-not $SkipTests) {
    Invoke-Step -Name 'test' -Arguments @(
        'test', $solution, '--no-build', '--configuration', $Configuration,
        '--logger', 'trx', '--logger', 'console;verbosity=normal',
        '--results-directory', (Join-Path $root 'artifacts/TestResults'))
}

Write-Summary
