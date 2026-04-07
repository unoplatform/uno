<#
.SYNOPSIS
Packs a local Uno.DevServer tool and runs the closest local repro flows for the DevServer CLI test suite.

.DESCRIPTION
This wrapper exists to reproduce DevServer CLI, MCP, and Codex validation failures locally without waiting on CI.
It can run the source-backed and package-backed E2E tests, then execute the same PowerShell CLI validation script
used by automation from a temporary snapshot.

Use this script when you want a reusable local repro harness for:
- DevServer CLI packaging issues
- MCP startup and selection-flow failures
- Codex-driven validation failures

Codex-only investigations typically use:
-SkipSourceE2E
-SkipPackageE2E

Required environment variables for Codex validation:
- OPENAI_API_KEY or CODEX_API_KEY

Useful optional environment variables:
- UNO_DEVSERVER_CODEX_MODEL
- UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS
- UNO_SKIP_CODEX_INTEGRATION

When the CLI validation fails, check the logged artifact paths for Codex stdout/stderr, unoapp MCP logs,
and any generated JSON snapshots preserved by run-devserver-cli-tests.ps1.

.EXAMPLE
pwsh -File build/test-scripts/run-devserver-cli-local.ps1 -Configuration Release

.EXAMPLE
pwsh -File build/test-scripts/run-devserver-cli-local.ps1 -Configuration Release -SkipSourceE2E -SkipPackageE2E

.EXAMPLE
$env:OPENAI_API_KEY='...'
$env:CODEX_API_KEY=$env:OPENAI_API_KEY
$env:UNO_DEVSERVER_CODEX_MODEL='gpt-5.3-codex'
pwsh -File build/test-scripts/run-devserver-cli-local.ps1 -SkipSourceE2E -SkipPackageE2E
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$PackagesDir = '',
    [switch]$SkipSourceE2E,
    [switch]$SkipPackageE2E,
    [switch]$SkipCliScript,
    [switch]$SkipCodex
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Log([string]$Message) { Write-Host "[devserver-cli-local] $Message" }

function Resolve-CompatibleUnoSdkVersion {
    $packagesRoot = Join-Path $HOME '.nuget' 'packages'
    $sdkRoot = Join-Path $packagesRoot 'uno.sdk.private'
    $devServerRoot = Join-Path $packagesRoot 'uno.winui.devserver'

    if (-not (Test-Path $sdkRoot) -or -not (Test-Path $devServerRoot)) {
        throw "Unable to locate local Uno SDK / DevServer packages under $packagesRoot."
    }

    $versions = Get-ChildItem -Path $sdkRoot -Directory |
        ForEach-Object { $_.Name } |
        Where-Object {
            Test-Path (Join-Path $devServerRoot $_ 'tools' 'rc' 'host' 'net10.0' 'Uno.UI.RemoteControl.Host.dll')
        } |
        Sort-Object -Descending

    $resolvedVersion = $versions | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($resolvedVersion)) {
        throw "Unable to find a locally cached Uno.Sdk.Private version with a net10.0 DevServer host."
    }

    return $resolvedVersion
}

function New-LocalDevPackageVersion {
    return "99.0.0-local-dev.$([System.DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..' '..')
$buildSourcesDirectory = $repoRoot.Path
$resolvedPackagesDir = if ([string]::IsNullOrWhiteSpace($PackagesDir)) {
    Join-Path $buildSourcesDirectory 'src' 'PackageCache'
}
else {
    $resolved = Resolve-Path -LiteralPath $PackagesDir -ErrorAction SilentlyContinue
    if ($resolved) { $resolved.Path } else { $PackagesDir }
}

if (-not (Test-Path $resolvedPackagesDir)) {
    New-Item -ItemType Directory -Path $resolvedPackagesDir -Force | Out-Null
}

$e2eProject = Join-Path $buildSourcesDirectory 'src' 'Uno.UI.DevServer.Cli.E2E.Tests' 'Uno.UI.DevServer.Cli.E2E.Tests.csproj'
$cliProject = Join-Path $buildSourcesDirectory 'src' 'Uno.UI.DevServer.Cli' 'Uno.UI.DevServer.Cli.csproj'
$cliScript = Join-Path $buildSourcesDirectory 'build' 'test-scripts' 'run-devserver-cli-tests.ps1'
$localPackageVersion = New-LocalDevPackageVersion

function New-TemporaryScriptSnapshot {
    param([string]$SourcePath)

    $snapshotPath = Join-Path $env:TEMP ("{0}-{1}.ps1" -f [System.IO.Path]::GetFileNameWithoutExtension($SourcePath), [Guid]::NewGuid().ToString("N"))
    $temporaryPath = "$snapshotPath.tmp"
    $content = [System.IO.File]::ReadAllText($SourcePath)
    [System.IO.File]::WriteAllText($temporaryPath, $content, [System.Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporaryPath -Destination $snapshotPath -Force
    return $snapshotPath
}

Write-Log "Packing Uno.DevServer into $resolvedPackagesDir"
& dotnet pack $cliProject --no-restore -c $Configuration -o $resolvedPackagesDir /p:Version=$localPackageVersion /p:PackageVersion=$localPackageVersion
if ($LASTEXITCODE -ne 0) {
    throw "dotnet pack failed with exit code $LASTEXITCODE"
}

$packedTool = Get-ChildItem -Path $resolvedPackagesDir -Filter "Uno.DevServer.$localPackageVersion.nupkg" |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not $packedTool) {
    throw "Unable to locate the packed Uno.DevServer package in $resolvedPackagesDir."
}

$packedVersion = $localPackageVersion

$compatibleSdkVersion = Resolve-CompatibleUnoSdkVersion

if (-not $SkipSourceE2E) {
    Write-Log "Running source-backed MCP E2E tests"
    & dotnet test --project $e2eProject --no-restore --filter TestCategory=DevServerSourceE2E -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Source-backed E2E tests failed with exit code $LASTEXITCODE"
    }
}

if (-not $SkipPackageE2E) {
    Write-Log "Running package-backed MCP E2E tests"
    & dotnet test --project $e2eProject --no-restore --filter TestCategory=DevServerPackageE2E -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Package-backed E2E tests failed with exit code $LASTEXITCODE"
    }
}

if (-not $SkipCliScript) {
    Write-Log "Running local DevServer CLI script validation"
    $previousBuildSourcesDirectory = $env:BUILD_SOURCESDIRECTORY
    $previousPackagesDir = $env:PACKAGES_DIR
    $previousSkipCodex = $env:UNO_SKIP_CODEX_INTEGRATION
    $previousNbgvSemVer2 = $env:NBGV_SemVer2
    $previousSkipLegacyStartupTests = $env:UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS
    $previousHttpDelaySeconds = $env:UNO_DEVSERVER_HTTP_DELAY_SECONDS
    $previousUserFilePollSeconds = $env:UNO_DEVSERVER_USERFILE_POLL_SECONDS
    $previousMaxAttempts = $env:UNO_DEVSERVER_MAX_ATTEMPTS
    $previousDevServerPackageVersion = $env:UNO_DEVSERVER_PACKAGE_VERSION
    $previousDevServerPackageSource = $env:UNO_DEVSERVER_PACKAGE_SOURCE

    $cliScriptSnapshot = $null
    try {
        $env:BUILD_SOURCESDIRECTORY = $buildSourcesDirectory
        $env:PACKAGES_DIR = $resolvedPackagesDir
        $env:NBGV_SemVer2 = $compatibleSdkVersion
        $env:UNO_DEVSERVER_PACKAGE_VERSION = $packedVersion
        $env:UNO_DEVSERVER_PACKAGE_SOURCE = $resolvedPackagesDir
        $env:UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS = 'true'
        $env:UNO_DEVSERVER_HTTP_DELAY_SECONDS = '2'
        $env:UNO_DEVSERVER_USERFILE_POLL_SECONDS = '2'
        $env:UNO_DEVSERVER_MAX_ATTEMPTS = '15'

        if ($SkipCodex) {
            $env:UNO_SKIP_CODEX_INTEGRATION = 'true'
        }
        else {
            Remove-Item Env:UNO_SKIP_CODEX_INTEGRATION -ErrorAction SilentlyContinue
        }

        $cliScriptSnapshot = New-TemporaryScriptSnapshot -SourcePath $cliScript
        Write-Log "Executing CLI validation from snapshot $cliScriptSnapshot"
        & pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File $cliScriptSnapshot
        if ($LASTEXITCODE -ne 0) {
            throw "run-devserver-cli-tests.ps1 failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        if ($null -ne $previousBuildSourcesDirectory) {
            $env:BUILD_SOURCESDIRECTORY = $previousBuildSourcesDirectory
        }
        else {
            Remove-Item Env:BUILD_SOURCESDIRECTORY -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousPackagesDir) {
            $env:PACKAGES_DIR = $previousPackagesDir
        }
        else {
            Remove-Item Env:PACKAGES_DIR -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousSkipCodex) {
            $env:UNO_SKIP_CODEX_INTEGRATION = $previousSkipCodex
        }
        else {
            Remove-Item Env:UNO_SKIP_CODEX_INTEGRATION -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousNbgvSemVer2) {
            $env:NBGV_SemVer2 = $previousNbgvSemVer2
        }
        else {
            Remove-Item Env:NBGV_SemVer2 -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousDevServerPackageVersion) {
            $env:UNO_DEVSERVER_PACKAGE_VERSION = $previousDevServerPackageVersion
        }
        else {
            Remove-Item Env:UNO_DEVSERVER_PACKAGE_VERSION -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousDevServerPackageSource) {
            $env:UNO_DEVSERVER_PACKAGE_SOURCE = $previousDevServerPackageSource
        }
        else {
            Remove-Item Env:UNO_DEVSERVER_PACKAGE_SOURCE -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousSkipLegacyStartupTests) {
            $env:UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS = $previousSkipLegacyStartupTests
        }
        else {
            Remove-Item Env:UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousHttpDelaySeconds) {
            $env:UNO_DEVSERVER_HTTP_DELAY_SECONDS = $previousHttpDelaySeconds
        }
        else {
            Remove-Item Env:UNO_DEVSERVER_HTTP_DELAY_SECONDS -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousUserFilePollSeconds) {
            $env:UNO_DEVSERVER_USERFILE_POLL_SECONDS = $previousUserFilePollSeconds
        }
        else {
            Remove-Item Env:UNO_DEVSERVER_USERFILE_POLL_SECONDS -ErrorAction SilentlyContinue
        }

        if ($null -ne $previousMaxAttempts) {
            $env:UNO_DEVSERVER_MAX_ATTEMPTS = $previousMaxAttempts
        }
        else {
            Remove-Item Env:UNO_DEVSERVER_MAX_ATTEMPTS -ErrorAction SilentlyContinue
        }

        if ($cliScriptSnapshot -and (Test-Path $cliScriptSnapshot)) {
            Remove-Item -LiteralPath $cliScriptSnapshot -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Log "Local DevServer validation completed successfully."
