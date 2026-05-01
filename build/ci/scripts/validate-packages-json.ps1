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
    [string]$PackagesJsonPath
)

$ErrorActionPreference = 'Stop'

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
                $statusCode = $_.Exception.Response.StatusCode.value__
                if ($statusCode -eq 404) {
                    Write-Host "  [MISSING] $pkg $version ($label) - NOT FOUND on NuGet" -ForegroundColor Red
                    $errors += "Group '$groupName': $pkg $version ($label) does not exist on NuGet.org"
                }
                else {
                    Write-Host "  [ERROR] $pkg $version ($label) - HTTP $statusCode" -ForegroundColor Yellow
                    $errors += "Group '$groupName': $pkg $version ($label) - HTTP error $statusCode"
                }
            }
        }
    }
}

Write-Host ""
Write-Host "Checked $checked package/version combinations." -ForegroundColor Cyan

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "VALIDATION FAILED - $($errors.Count) error(s):" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}
else {
    Write-Host "All packages validated successfully." -ForegroundColor Green
    exit 0
}
