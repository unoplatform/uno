#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that all package IDs and versions in src/Uno.Sdk/packages.json exist on NuGet.org,
    and that the pinned Xamarin.* package set is mutually dependency-coherent.

.DESCRIPTION
    Parses packages.json, skips groups with placeholder versions (e.g. "DefaultUnoVersion"),
    and verifies each package ID + version (including versionOverride entries) against NuGet.org.
    Then, for each TFM pin set (base and each versionOverride TFM), walks the nuspec dependency
    floors of every pinned Xamarin.* package (recursing through unpinned intermediates) and fails
    when a pinned package is below a floor required by another pin - the NU1605 downgrade class
    that broke the AndroidWear feature (#23991).
    Exits with code 1 if any package/version is missing or any dependency floor is violated.

.PARAMETER PackagesJsonPath
    Path to the packages.json file. Defaults to src/Uno.Sdk/packages.json relative to repo root.
#>
param(
    [string]$PackagesJsonPath,
    [switch]$WarningOnly
)

$ErrorActionPreference = 'Stop'

# Auto-detect: only fail on master or PRs targeting master.
# All other branches (release, feature, dev) use warning-only mode since they
# may reference packages not yet published to NuGet.org.
if (-not $WarningOnly) {
    $branch = $env:BUILD_SOURCEBRANCH
    $targetBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH
    $isMaster = $branch -eq 'refs/heads/master'
    $isPRToMaster = $targetBranch -eq 'refs/heads/master'

    if (-not $isMaster -and -not $isPRToMaster) {
        Write-Host "Non-master branch (source: $branch, target: $targetBranch) - running in warning-only mode." -ForegroundColor Yellow
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
$validatedPairs = @{}  # De-duplicate package+version pairs across groups

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

            # De-duplicate: skip if we already validated this package+version
            $pairKey = "$($pkg.ToLowerInvariant())|$($version.ToLowerInvariant())"
            if ($validatedPairs.ContainsKey($pairKey)) {
                Write-Host "  [OK] $pkg $version ($label) - already validated" -ForegroundColor DarkGreen
                continue
            }
            $validatedPairs[$pairKey] = $true

            $checked++
            $url = "https://api.nuget.org/v3-flatcontainer/$($pkg.ToLowerInvariant())/$($version.ToLowerInvariant())/$($pkg.ToLowerInvariant()).nuspec"

            try {
                $response = Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing `
                    -TimeoutSec 30 -MaximumRetryCount 3 -RetryIntervalSec 2 -ErrorAction Stop
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

# ---------------------------------------------------------------------------
# Dependency coherence: every pinned Xamarin.* package's nuspec dependency
# floors must be satisfied by the other pins of the same TFM set, otherwise
# consumers restore with NU1605 (warning-as-error) downgrade failures.
# ---------------------------------------------------------------------------

$script:nuspecCache = @{}
$script:reportedConflicts = [System.Collections.Generic.HashSet[string]]::new()

function Get-NuspecXml([string]$Id, [string]$Version) {
    $key = "$($Id.ToLowerInvariant())|$($Version.ToLowerInvariant())"
    if ($script:nuspecCache.ContainsKey($key)) { return $script:nuspecCache[$key] }
    $url = "https://api.nuget.org/v3-flatcontainer/$($Id.ToLowerInvariant())/$($Version.ToLowerInvariant())/$($Id.ToLowerInvariant()).nuspec"
    $xml = $null
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30 `
            -MaximumRetryCount 3 -RetryIntervalSec 2 -ErrorAction Stop
        $xml = [xml]$response.Content
    }
    catch {
        Write-Host "  [WARN] Could not fetch nuspec for $Id $Version - skipping its subtree" -ForegroundColor Yellow
    }
    $script:nuspecCache[$key] = $xml
    return $xml
}

# Lower bound of a nuspec version range ("1.4.0.5", "[1.4.0.1, 1.4.1)", ...), or $null.
function Get-MinVersion([string]$Range) {
    if ([string]::IsNullOrWhiteSpace($Range)) { return $null }
    $v = $Range.Trim()
    if ($v.StartsWith('[') -or $v.StartsWith('(')) {
        $v = $v.TrimStart('[', '(').Split(',')[0].Trim()
        if (-not $v) { return $null }
    }
    $v = $v.Split('-')[0].Split('+')[0]
    if ($v -notmatch '\.') { $v = "$v.0" }
    try { return [version]$v } catch { return $null }
}

# Pick the nuspec dependency group matching the TFM pin set being validated.
function Select-DependencyGroup($NuspecXml, [string]$TfmPrefix) {
    $groups = @($NuspecXml.package.metadata.dependencies.group) | Where-Object { $_ }
    if (-not $groups) { return $null }
    $exact = $groups | Where-Object { $_.targetFramework -like "$TfmPrefix-android*" } | Select-Object -First 1
    if ($exact) { return $exact }
    $requestedMajor = [int]([regex]::Match($TfmPrefix, '\d+').Value)
    $android = $groups |
        Where-Object { $_.targetFramework -match '^net(\d+)\.0-android' } |
        Sort-Object { [int][regex]::Match($_.targetFramework, '^net(\d+)').Groups[1].Value }
    if (-not $android) { return $groups[0] }
    $below = @($android | Where-Object { [int][regex]::Match($_.targetFramework, '^net(\d+)').Groups[1].Value -le $requestedMajor })
    if ($below) { return $below[-1] }
    return $android[0]
}

# Walk dependency floors of ($Id, $Version): a pinned dependency must satisfy its floor
# (NuGet uses the pin from there on, so recursion stops); an unpinned Xamarin.* dependency
# resolves to at least its floor, so recurse through it at the floor version.
function Test-DependencyFloors($Pinned, [string]$SetLabel, [string]$TfmPrefix, [string]$Id, [string]$Version, [string]$Chain, [int]$Depth, $Visited) {
    if ($Depth -gt 8) { return }
    $key = "$($Id.ToLowerInvariant())|$Version"
    if (-not $Visited.Add($key)) { return }
    $nuspec = Get-NuspecXml $Id $Version
    if (-not $nuspec) { return }
    $depGroup = Select-DependencyGroup $nuspec $TfmPrefix
    if (-not $depGroup) { return }
    foreach ($dep in @($depGroup.dependency)) {
        if (-not $dep -or -not $dep.id) { continue }
        $floor = Get-MinVersion $dep.version
        if (-not $floor) { continue }
        $depIdLower = $dep.id.ToLowerInvariant()
        if ($Pinned.ContainsKey($depIdLower)) {
            $pinnedVersion = Get-MinVersion $Pinned[$depIdLower]
            if ($pinnedVersion -and $pinnedVersion -lt $floor) {
                # Report each violated (package, floor) once per set; extra chains add noise, not information.
                $conflictKey = "$SetLabel|$depIdLower|$floor"
                if ($script:reportedConflicts.Add($conflictKey)) {
                    Write-Host "  [CONFLICT] ($SetLabel) $Chain -> $($dep.id) >= $($dep.version), but pinned at $($Pinned[$depIdLower])" -ForegroundColor Red
                    $script:errors += "Dependency conflict ($SetLabel): $Chain requires $($dep.id) >= $($dep.version), but packages.json pins $($Pinned[$depIdLower])"
                }
            }
        }
        elseif ($dep.id.StartsWith('Xamarin.', [System.StringComparison]::OrdinalIgnoreCase)) {
            Test-DependencyFloors $Pinned $SetLabel $TfmPrefix $dep.id $floor.ToString() "$Chain -> $($dep.id) $floor" ($Depth + 1) $Visited
        }
    }
}

# Effective pin sets: base versions (net9.0 apps) plus one set per versionOverride TFM.
$overrideTfms = @()
foreach ($group in $json) {
    if ($group.PSObject.Properties['versionOverride'] -and $null -ne $group.versionOverride) {
        $overrideTfms += $group.versionOverride.PSObject.Properties.Name
    }
}
$tfmSets = @(@{ Label = 'base/net9.0'; Tfm = 'net9.0'; OverrideKey = $null })
foreach ($tfm in ($overrideTfms | Sort-Object -Unique)) {
    $tfmSets += @{ Label = $tfm; Tfm = $tfm; OverrideKey = $tfm }
}

$coherenceChecked = 0
foreach ($set in $tfmSets) {
    $pinned = @{}
    foreach ($group in $json) {
        $effective = $group.version
        if ($set.OverrideKey -and $group.PSObject.Properties['versionOverride'] -and $null -ne $group.versionOverride) {
            $override = $group.versionOverride.PSObject.Properties[$set.OverrideKey]
            if ($override) { $effective = $override.Value }
        }
        if ($placeholderVersions -contains $effective) { continue }
        foreach ($pkg in $group.packages) {
            $pinned[$pkg.ToLowerInvariant()] = $effective
        }
    }

    Write-Host ""
    Write-Host "Validating dependency coherence for pin set '$($set.Label)'..." -ForegroundColor Cyan
    foreach ($group in $json) {
        foreach ($pkg in $group.packages) {
            if (-not $pkg.StartsWith('Xamarin.', [System.StringComparison]::OrdinalIgnoreCase)) { continue }
            $version = $pinned[$pkg.ToLowerInvariant()]
            if (-not $version) { continue }
            $coherenceChecked++
            $visited = [System.Collections.Generic.HashSet[string]]::new()
            Test-DependencyFloors $pinned $set.Label $set.Tfm $pkg $version "$pkg $version" 0 $visited
        }
    }
}

Write-Host ""
Write-Host "Checked dependency coherence for $coherenceChecked pinned package/set combinations." -ForegroundColor Cyan

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
