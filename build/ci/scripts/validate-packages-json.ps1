#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that all package IDs and versions in src/Uno.Sdk/packages.json exist on NuGet.org.

.DESCRIPTION
    Parses packages.json, skips groups with placeholder versions (e.g. "DefaultUnoVersion"),
    and verifies each package ID + version (including versionOverride entries) against NuGet.org.
    Exits with code 1 if any package/version is missing.

.PARAMETER PackagesJsonPath
    Path to the packages.json file. Defaults to src/Uno.Sdk/packages.json relative to repo root.
#>
param(
    [string]$PackagesJsonPath,
    [switch]$WarningOnly
)

$ErrorActionPreference = 'Stop'

# Auto-detect: on release/* branches, switch to warning-only mode
# (stable builds test with versions not yet public on NuGet.org)
if (-not $WarningOnly) {
    $branch = $env:BUILD_SOURCEBRANCH
    if ($branch -and $branch -like 'refs/heads/release/*') {
        Write-Host "Detected release branch ($branch) - running in warning-only mode." -ForegroundColor Yellow
        $WarningOnly = $true
    }
}

# Resolve path
if (-not $PackagesJsonPath) {
    $repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
    $PackagesJsonPath = Join-Path $repoRoot 'src/Uno.Sdk/packages.json'
}

if (-not (Test-Path $PackagesJsonPath)) {
    Write-Error "packages.json not found at: $PackagesJsonPath"
    exit 1
}

Write-Host "Validating packages.json: $PackagesJsonPath" -ForegroundColor Cyan

$json = Get-Content $PackagesJsonPath -Raw | ConvertFrom-Json
$errors = @()
$checked = 0

# Placeholder versions that should not be validated against NuGet
$placeholderVersions = @('DefaultUnoVersion')

foreach ($group in $json) {
    $groupName = $group.group
    $baseVersion = $group.version
    $packages = $group.packages

    # Skip groups with placeholder versions
    if ($placeholderVersions -contains $baseVersion) {
        Write-Host "  [SKIP] $groupName (placeholder version: $baseVersion)" -ForegroundColor DarkGray
        continue
    }

    # Collect all version+package combinations to check
    $versionsToCheck = @()

    # Base version
    $versionsToCheck += @{ Version = $baseVersion; Label = 'base' }

    # Version overrides (TFM-specific)
    if ($group.PSObject.Properties['versionOverride'] -and $null -ne $group.versionOverride) {
        $overrides = $group.versionOverride
        foreach ($prop in $overrides.PSObject.Properties) {
            $versionsToCheck += @{ Version = $prop.Value; Label = "override($($prop.Name))" }
        }
    }

    foreach ($pkg in $packages) {
        foreach ($vEntry in $versionsToCheck) {
            $version = $vEntry.Version
            $label = $vEntry.Label

            # Also skip overrides that reference a placeholder
            if ($placeholderVersions -contains $version) {
                continue
            }

            $checked++
            $url = "https://api.nuget.org/v3-flatcontainer/$($pkg.ToLowerInvariant())/$($version.ToLowerInvariant())/$($pkg.ToLowerInvariant()).nuspec"

            try {
                $response = Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing -ErrorAction Stop
                if ($response.StatusCode -eq 200) {
                    Write-Host "  [OK] $pkg $version ($label)" -ForegroundColor Green
                }
            }
            catch {
                $httpResponse = $_.Exception.Response
                if ($null -ne $httpResponse) {
                    $statusCode = $httpResponse.StatusCode.value__
                    if ($statusCode -eq 404) {
                        Write-Host "  [MISSING] $pkg $version ($label) - NOT FOUND on NuGet" -ForegroundColor Red
                        $errors += "Group '$groupName': $pkg $version ($label) does not exist on NuGet.org"
                    }
                    else {
                        Write-Host "  [ERROR] $pkg $version ($label) - HTTP $statusCode" -ForegroundColor Yellow
                        $errors += "Group '$groupName': $pkg $version ($label) - HTTP error $statusCode"
                    }
                }
                else {
                    Write-Host "  [ERROR] $pkg $version ($label) - $($_.Exception.Message)" -ForegroundColor Yellow
                    $errors += "Group '$groupName': $pkg $version ($label) - network error: $($_.Exception.Message)"
                }
            }
        }
    }
}

Write-Host ""
Write-Host "Checked $checked package/version combinations." -ForegroundColor Cyan

if ($errors.Count -gt 0) {
    Write-Host ""
    if ($WarningOnly) {
        Write-Host "VALIDATION WARNINGS - $($errors.Count) package(s) not found on NuGet.org (non-fatal):" -ForegroundColor Yellow
    }
    else {
        Write-Host "VALIDATION FAILED - $($errors.Count) error(s):" -ForegroundColor Red
    }
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor $(if ($WarningOnly) { 'Yellow' } else { 'Red' })
    }
    if ($WarningOnly) {
        Write-Host ""
        Write-Host "Running in warning-only mode (stable branch) - not failing the build." -ForegroundColor Yellow
        exit 0
    }
    exit 1
}
else {
    Write-Host "All packages validated successfully." -ForegroundColor Green
    exit 0
}
