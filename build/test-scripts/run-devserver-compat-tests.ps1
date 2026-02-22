# Backward-compatibility tests for DevServer CLI discovery against older Uno.Sdk releases.
# Downloads SDK packages directly from NuGet (no dotnet restore needed) and validates
# that the current CLI can parse their packages.json and locate the DevServer host.

[CmdletBinding()]
param(
    [string]$DevServerCliDllPath,
    [string[]]$SdkVersionFilter  # Optional: filter by label (e.g. '6.5-latest')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ────────────────────────────────────────────────────────────────────
# Version matrix
# ────────────────────────────────────────────────────────────────────

$script:VersionMatrix = @(
    @{ Label = '5.1-oldest'; SdkVersion = '5.1.31';  ExpectHostPath = $false }
    @{ Label = '5.6-latest'; SdkVersion = '5.6.54';  ExpectHostPath = $false }
    @{ Label = '6.0-oldest'; SdkVersion = '6.0.67';  ExpectHostPath = $false }
    @{ Label = '6.2-mid';    SdkVersion = '6.2.39';  ExpectHostPath = $false }
    @{ Label = '6.5-latest'; SdkVersion = '6.5.31';  ExpectHostPath = $true  }
)

# ────────────────────────────────────────────────────────────────────
# Infrastructure helpers (same patterns as run-devserver-cli-tests.ps1)
# ────────────────────────────────────────────────────────────────────

function Write-Log([string]$Message) { Write-Host "[devserver-compat] $Message" }

$script:DevServerHostExecutable = "dotnet"
$script:DevServerCliEntryPoint  = "uno-devserver"
$script:DevServerCliUsesDllPath = $false
$script:DevServerCliResolvedDllPath = $null

if (-not [string]::IsNullOrWhiteSpace($DevServerCliDllPath)) {
    if (-not (Test-Path -LiteralPath $DevServerCliDllPath)) {
        throw "Provided DevServerCliDllPath '$DevServerCliDllPath' was not found."
    }
    $script:DevServerCliResolvedDllPath = (Resolve-Path -LiteralPath $DevServerCliDllPath -ErrorAction Stop).Path
    $script:DevServerCliEntryPoint = $script:DevServerCliResolvedDllPath
    $script:DevServerCliUsesDllPath = $true
}

function Get-DevserverCliArguments {
    param([string[]]$Arguments = @())
    return @($script:DevServerCliEntryPoint) + $Arguments
}

function Invoke-DevserverCli {
    param([string[]]$Arguments = @())
    $fullArgs = Get-DevserverCliArguments -Arguments $Arguments
    & $script:DevServerHostExecutable @fullArgs
}

# ────────────────────────────────────────────────────────────────────
# NuGet helpers
# ────────────────────────────────────────────────────────────────────

function Get-NuGetCachePath {
    if (-not [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
        return $env:NUGET_PACKAGES
    }
    return (Join-Path $HOME '.nuget' 'packages')
}

function Install-NuGetPackageDirect {
    param(
        [Parameter(Mandatory)][string]$PackageId,
        [Parameter(Mandatory)][string]$Version
    )

    $cachePath = Join-Path (Get-NuGetCachePath) $PackageId.ToLower() $Version

    # Idempotent: skip if already present
    if (Test-Path $cachePath) {
        Write-Log "  Package $PackageId $Version already in cache."
        return $cachePath
    }

    $lowerId  = $PackageId.ToLower()
    $url      = "https://api.nuget.org/v3-flatcontainer/$lowerId/$Version/$lowerId.$Version.nupkg"
    $tempDir  = Join-Path ([System.IO.Path]::GetTempPath()) "compat-nupkg-$lowerId-$Version"
    $tempFile = "$tempDir.nupkg"

    Write-Log "  Downloading $PackageId $Version from NuGet..."
    try {
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $url -OutFile $tempFile -UseBasicParsing
    }
    catch {
        throw "Failed to download $PackageId $Version from NuGet: $($_.Exception.Message)"
    }

    # Extract
    if (Test-Path $tempDir) { Remove-Item -Recurse -Force $tempDir }
    Expand-Archive -Path $tempFile -DestinationPath $tempDir -Force

    # Move to cache location
    $parentDir = Split-Path $cachePath -Parent
    if (-not (Test-Path $parentDir)) {
        New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
    }
    Move-Item -Path $tempDir -Destination $cachePath -Force

    # Create .nupkg.metadata sentinel so NuGet recognizes the package
    $metadataPath = Join-Path $cachePath '.nupkg.metadata'
    @"
{
  "version": 2,
  "contentHash": "",
  "source": "https://api.nuget.org/v3/index.json"
}
"@ | Set-Content -Path $metadataPath -Encoding UTF8

    # Cleanup temp nupkg file
    if (Test-Path $tempFile) { Remove-Item -Force $tempFile }

    Write-Log "  Installed $PackageId $Version to cache."
    return $cachePath
}

function Read-PackagesJson {
    param([Parameter(Mandatory)][string]$SdkCachePath)

    $packagesJsonPath = Join-Path $SdkCachePath 'targets' 'netstandard2.0' 'packages.json'
    if (-not (Test-Path $packagesJsonPath)) {
        Write-Log "  packages.json not found at $packagesJsonPath (old SDK layout)"
        return @{}
    }

    $content = Get-Content -Raw -Path $packagesJsonPath
    $parsed  = $content | ConvertFrom-Json

    $result = @{}
    foreach ($group in $parsed) {
        $version = $group.version
        foreach ($pkg in $group.packages) {
            $result[$pkg.ToLower()] = $version
        }
    }
    return $result
}

function Install-SdkAndDependencies {
    param([Parameter(Mandatory)][string]$SdkVersion)

    Write-Log "Installing Uno.Sdk $SdkVersion and dependencies..."

    $sdkPath = Install-NuGetPackageDirect -PackageId 'Uno.Sdk' -Version $SdkVersion

    $packages = Read-PackagesJson -SdkCachePath $sdkPath

    $devServerVersion = $packages['uno.winui.devserver']
    if ([string]::IsNullOrWhiteSpace($devServerVersion)) {
        Write-Log "  Uno.WinUI.DevServer not referenced in packages.json (old SDK)"
        return @{
            SdkPath           = $sdkPath
            DevServerVersion  = $null
        }
    }

    Write-Log "  DevServer version from packages.json: $devServerVersion"
    Install-NuGetPackageDirect -PackageId 'Uno.WinUI.DevServer' -Version $devServerVersion | Out-Null

    return @{
        SdkPath           = $sdkPath
        DevServerVersion  = $devServerVersion
    }
}

# ────────────────────────────────────────────────────────────────────
# Test directory setup
# ────────────────────────────────────────────────────────────────────

function New-CompatTestDirectory {
    param(
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$SdkVersion
    )

    $testDir = Join-Path ([System.IO.Path]::GetTempPath()) "compat-$Label"
    if (Test-Path $testDir) { Remove-Item -Recurse -Force $testDir }
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null

    $globalJson = @{
        'msbuild-sdks' = @{
            'Uno.Sdk' = $SdkVersion
        }
    } | ConvertTo-Json -Depth 5

    Set-Content -Path (Join-Path $testDir 'global.json') -Value $globalJson -Encoding UTF8

    return $testDir
}

# ────────────────────────────────────────────────────────────────────
# Tier assertions
# ────────────────────────────────────────────────────────────────────

function Assert-T0-ValidJson {
    param(
        [Parameter(Mandatory)][string]$RawOutput,
        [Parameter(Mandatory)][int]$ExitCode
    )

    # Exit code 0 (success) or 1 (partial, e.g. host not found) are acceptable.
    # Anything else indicates a crash.
    if ($ExitCode -gt 1) {
        throw "T0 FAIL: CLI exited with code $ExitCode (crash). Output:`n$RawOutput"
    }

    # Strip non-JSON lines (e.g. "warn:" log lines on stderr captured by 2>&1)
    $jsonText = ($RawOutput -split "`n" | Where-Object { $_ -match '^\s*[\{\[\"]' -or $_ -match '^\s*\}' -or $_ -match '^\s*\]' -or $_ -match '^\s*"' } ) -join "`n"
    if ([string]::IsNullOrWhiteSpace($jsonText)) {
        $jsonText = $RawOutput
    }

    try {
        $json = $jsonText | ConvertFrom-Json
    }
    catch {
        throw "T0 FAIL: CLI output is not valid JSON. Output:`n$RawOutput"
    }

    return $json
}

function Assert-T1-SdkIdentity {
    param(
        [Parameter(Mandatory)]$Json,
        [Parameter(Mandatory)][string]$ExpectedVersion
    )

    if ($Json.unoSdkPackage -ne 'Uno.Sdk') {
        throw "T1 FAIL: unoSdkPackage is '$($Json.unoSdkPackage)', expected 'Uno.Sdk'"
    }
    if ($Json.unoSdkVersion -ne $ExpectedVersion) {
        throw "T1 FAIL: unoSdkVersion is '$($Json.unoSdkVersion)', expected '$ExpectedVersion'"
    }
}

function Assert-T2-SdkPath {
    param([Parameter(Mandatory)]$Json)

    if ([string]::IsNullOrWhiteSpace($Json.unoSdkPath)) {
        throw "T2 FAIL: unoSdkPath is null or empty"
    }
    if (-not [string]::IsNullOrWhiteSpace($Json.packagesJsonPath)) {
        if (-not (Test-Path $Json.packagesJsonPath)) {
            throw "T2 FAIL: packagesJsonPath '$($Json.packagesJsonPath)' does not exist"
        }
    }
    else {
        throw "T2 FAIL: packagesJsonPath is null or empty"
    }
}

function Assert-T3-DevServerVersion {
    param([Parameter(Mandatory)]$Json)

    if ([string]::IsNullOrWhiteSpace($Json.devServerPackageVersion)) {
        throw "T3 FAIL: devServerPackageVersion is null or empty"
    }
}

function Assert-T4-DevServerPath {
    param([Parameter(Mandatory)]$Json)

    if ([string]::IsNullOrWhiteSpace($Json.devServerPackagePath)) {
        throw "T4 FAIL: devServerPackagePath is null or empty"
    }
    if (-not (Test-Path $Json.devServerPackagePath)) {
        throw "T4 FAIL: devServerPackagePath '$($Json.devServerPackagePath)' does not exist"
    }
}

function Assert-T5-HostPath {
    param(
        [Parameter(Mandatory)]$Json,
        [Parameter(Mandatory)][bool]$ExpectHostPath
    )

    if ($ExpectHostPath) {
        if ([string]::IsNullOrWhiteSpace($Json.hostPath)) {
            throw "T5 FAIL: hostPath is null or empty (expected to exist)"
        }
        if (-not (Test-Path $Json.hostPath)) {
            throw "T5 FAIL: hostPath '$($Json.hostPath)' does not exist"
        }
    }
    # When ExpectHostPath is false, we skip - missing host is expected for old SDKs
}

function Assert-ErrorsAndWarnings {
    param(
        [Parameter(Mandatory)]$Json,
        [Parameter(Mandatory)][hashtable]$VersionEntry
    )

    $errors = @()
    if ($null -ne $Json.errors) {
        $errors = @($Json.errors)
    }

    if ($VersionEntry.ExpectHostPath) {
        # For versions where host should be found, no errors expected
        if ($errors.Count -gt 0) {
            throw "ERRORS FAIL: Unexpected errors for $($VersionEntry.Label): $($errors -join '; ')"
        }
    }
    else {
        # For older versions, expected errors include missing packages.json, missing DevServer, etc.
        foreach ($err in $errors) {
            if ($err -notmatch 'host not found|not found in package|packages\.json not found|version not found in packages') {
                throw "ERRORS FAIL: Unexpected error for $($VersionEntry.Label): $err"
            }
        }
    }
}

# ────────────────────────────────────────────────────────────────────
# Test orchestration for a single version
# ────────────────────────────────────────────────────────────────────

function Test-DiscoCompatVersion {
    param([Parameter(Mandatory)][hashtable]$VersionEntry)

    $label   = $VersionEntry.Label
    $version = $VersionEntry.SdkVersion

    Write-Log "────────────────────────────────────────────"
    Write-Log "Testing $label (Uno.Sdk $version)"
    Write-Log "────────────────────────────────────────────"

    # 1. Install SDK + DevServer packages into NuGet cache
    $installed = Install-SdkAndDependencies -SdkVersion $version

    # 2. Create test directory with global.json
    $testDir = New-CompatTestDirectory -Label $label -SdkVersion $version

    try {
        # 3. Run disco --json
        Write-Log "Running disco --json --solution-dir $testDir"
        $rawOutput = (Invoke-DevserverCli -Arguments @('disco', '--json', '--solution-dir', $testDir)) 2>&1
        $exitCode  = $LASTEXITCODE

        # Flatten output to string
        $outputText = ($rawOutput | Out-String).Trim()
        Write-Log "CLI exit code: $exitCode"
        Write-Log "CLI output:`n$outputText"

        # 4. Run tier assertions
        $json = Assert-T0-ValidJson -RawOutput $outputText -ExitCode $exitCode
        Assert-T1-SdkIdentity      -Json $json -ExpectedVersion $version

        if ($VersionEntry.ExpectHostPath) {
            # Full assertions only for SDKs that have packages.json
            Assert-T2-SdkPath           -Json $json
            Assert-T3-DevServerVersion  -Json $json
            Assert-T4-DevServerPath     -Json $json
            Assert-T5-HostPath          -Json $json -ExpectHostPath $true
        }

        Assert-ErrorsAndWarnings    -Json $json -VersionEntry $VersionEntry

        Write-Log "PASS: $label"
        return $true
    }
    finally {
        # Cleanup test directory
        if (Test-Path $testDir) {
            Remove-Item -Recurse -Force $testDir -ErrorAction SilentlyContinue
        }
    }
}

# ────────────────────────────────────────────────────────────────────
# CLI tool installation (same as existing script)
# ────────────────────────────────────────────────────────────────────

function Install-DevServerCliTool {
    if ($script:DevServerCliUsesDllPath) {
        Write-Log "Using devserver CLI from DLL path: $script:DevServerCliResolvedDllPath"
        return
    }

    $nbgvVersion = $env:NBGV_SemVer2
    if ([string]::IsNullOrWhiteSpace($nbgvVersion)) {
        Write-Log "NBGV_SemVer2 not set, assuming uno-devserver CLI is already installed."
        return
    }

    Write-Log "Installing uno-devserver CLI tool version $nbgvVersion..."
    & dotnet tool install -g uno.devserver --version $nbgvVersion
    if ($LASTEXITCODE -ne 0) {
        # Try update if already installed
        & dotnet tool update -g uno.devserver --version $nbgvVersion
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install/update uno-devserver CLI tool."
        }
    }
}

# ────────────────────────────────────────────────────────────────────
# Main
# ────────────────────────────────────────────────────────────────────

Write-Log "============================================"
Write-Log "DevServer CLI Backward-Compatibility Tests"
Write-Log "============================================"

Install-DevServerCliTool

$anyFailure   = $false
$passCount    = 0
$failCount    = 0
$skipCount    = 0
$results      = @()

foreach ($entry in $script:VersionMatrix) {
    $label = $entry.Label

    # Apply filter if specified
    if ($SdkVersionFilter -and $SdkVersionFilter.Count -gt 0) {
        $matched = $false
        foreach ($filter in $SdkVersionFilter) {
            if ($label -like "*$filter*") { $matched = $true; break }
        }
        if (-not $matched) {
            Write-Log "SKIP: $label (filtered out)"
            $skipCount++
            $results += @{ Label = $label; Status = 'SKIP'; Error = '' }
            continue
        }
    }

    try {
        $passed = Test-DiscoCompatVersion -VersionEntry $entry
        $passCount++
        $results += @{ Label = $label; Status = 'PASS'; Error = '' }
    }
    catch {
        Write-Log "FAIL: $label - $($_.Exception.Message)"
        $failCount++
        $anyFailure = $true
        $results += @{ Label = $label; Status = 'FAIL'; Error = $_.Exception.Message }
    }
}

# ────────────────────────────────────────────────────────────────────
# Summary
# ────────────────────────────────────────────────────────────────────

Write-Log ""
Write-Log "============================================"
Write-Log "Summary"
Write-Log "============================================"

foreach ($r in $results) {
    $statusTag = switch ($r.Status) {
        'PASS' { 'PASS' }
        'FAIL' { "FAIL: $($r.Error)" }
        'SKIP' { 'SKIP' }
    }
    Write-Log "  $($r.Label): $statusTag"
}

Write-Log ""
Write-Log "Passed: $passCount  Failed: $failCount  Skipped: $skipCount"

if ($anyFailure) {
    Write-Log "One or more compatibility tests FAILED."
    exit 1
}

Write-Log "All compatibility tests passed."
exit 0
