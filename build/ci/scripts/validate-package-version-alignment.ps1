#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fails when a package version written by hand outside src/Directory.Packages.props drifts from
    the version Uno is built against.

.DESCRIPTION
    Several files repeat the versions of Uno's dependencies, and a version bump must change all of
    them together:

      - src/Directory.Packages.props: the versions Uno is built against (the source of truth).
      - build/nuget/*.nuspec: the dependency floors of the packed Uno packages. An app resolves
        these floors, so a floor below the build version gives the app an older package than the
        one Uno was compiled against.
      - src/Uno.Sdk/packages.json: the Uno.Sdk.Private manifest, which pins the packages the
        Uno.Sdk references directly. A pin below a nuspec floor fails the app's restore (NU1605).
      - build/**: CI scripts passing -p:SkiaSharpVersion=... explicitly.

    Checks:
      1. Every nuspec dependency on a package Uno is built against declares that same version
         (except the documented $KnownFloorExceptions).
      2. Every Uno.Sdk.Private manifest pin is at or above the nuspec floor of the same package.
      3. SkiaSharp and HarfBuzzSharp match exactly everywhere: Directory.Packages.props uses the
         shared property, and the nuspecs, the manifest and CI scripts use its value. SkiaSharp
         checks its native library at startup, and an app whose managed SkiaSharp and native
         libSkiaSharp differ fails with "The version of the native libSkiaSharp library (...) is
         incompatible with this version of SkiaSharp".

    Unlike validate-packages-json.ps1, this check needs no network and fails on every branch.

.PARAMETER RepoRoot
    Repository root. Defaults to three levels above this script.
#>
param(
    [string]$RepoRoot = (Join-Path $PSScriptRoot '../../..')
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path

# Package families whose managed and native packages must have the exact same version everywhere.
# Prefix = package id prefix; Property = the Directory.Packages.props property holding the version.
# Note: PowerShell variable names are case-insensitive, so no other variable may be named $families.
$Families = @(
    @{ Prefix = 'SkiaSharp';     Property = 'SkiaSharpVersion' }
    @{ Prefix = 'HarfBuzzSharp'; Property = 'HarfbuzzSharpVersion' }
)

# Nuspec floors deliberately below the build version: package id = the allowed floor, with the reason.
$KnownFloorExceptions = @{
    # Uno.UI.Svg only uses Svg.Skia's SKSvg (constructor, FromSvg, Picture), present in 3.0.6, and
    # the public Uno.Sdk 7.0 pins Svg.Skia 3.0.6: raising this floor without raising that pin in the
    # same Uno.Sdk makes Android/iOS restores fail with NU1605.
    'Svg.Skia' = '3.0.6'
}

$errors = [System.Collections.Generic.List[string]]::new()
$checked = 0

function Get-RelativePath([string]$Path) {
    [System.IO.Path]::GetRelativePath($RepoRoot, $Path).Replace('\', '/')
}

# A nuspec dependency version can be a range ("[1.0]", "[1.0,2.0)"): use its lower bound.
function Get-LowerBound([string]$Version) {
    $v = $Version.Trim().TrimStart('[', '(')
    ($v -split ',')[0].TrimEnd(']', ')').Trim()
}

# NuGet version order: numeric parts first, then a release sorts above its prereleases, and
# prerelease labels compare part by part (numbers numerically, below text).
function Compare-PackageVersion([string]$A, [string]$B) {
    $pa = $A -split '[-+]', 2; $pb = $B -split '[-+]', 2
    $na = @($pa[0] -split '\.' | ForEach-Object { [long]$_ }); $nb = @($pb[0] -split '\.' | ForEach-Object { [long]$_ })
    for ($i = 0; $i -lt [Math]::Max($na.Count, $nb.Count); $i++) {
        $x = if ($i -lt $na.Count) { $na[$i] } else { 0 }; $y = if ($i -lt $nb.Count) { $nb[$i] } else { 0 }
        if ($x -ne $y) { return [Math]::Sign($x - $y) }
    }
    $ra = if ($pa.Count -gt 1) { $pa[1] } else { '' }; $rb = if ($pb.Count -gt 1) { $pb[1] } else { '' }
    if ($ra -eq $rb) { return 0 }
    if ($ra -eq '') { return 1 }
    if ($rb -eq '') { return -1 }
    $la = $ra -split '\.'; $lb = $rb -split '\.'
    for ($i = 0; $i -lt [Math]::Max($la.Count, $lb.Count); $i++) {
        if ($i -ge $la.Count) { return -1 }
        if ($i -ge $lb.Count) { return 1 }
        $ia = $la[$i] -match '^\d+$'; $ib = $lb[$i] -match '^\d+$'
        if ($ia -and $ib) { $c = [Math]::Sign([long]$la[$i] - [long]$lb[$i]) }
        elseif ($ia) { $c = -1 }
        elseif ($ib) { $c = 1 }
        else { $c = [Math]::Sign([string]::CompareOrdinal($la[$i], $lb[$i])) }
        if ($c -ne 0) { return $c }
    }
    return 0
}

function Get-Family([string]$PackageId) {
    foreach ($family in $Families) {
        if ($PackageId -eq $family.Prefix -or $PackageId.StartsWith("$($family.Prefix).", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $family
        }
    }
    return $null
}

# --- Source of truth: src/Directory.Packages.props ---------------------------------------------
$propsPath = Join-Path $RepoRoot 'src/Directory.Packages.props'
if (-not (Test-Path $propsPath)) { throw "Not found: $propsPath" }
$propsLines = Get-Content $propsPath
$propsText = $propsLines -join "`n"

$properties = @{}
foreach ($m in [regex]::Matches($propsText, '<(\w+)>\s*([^<\s]+)\s*</\1>')) {
    if (-not $properties.ContainsKey($m.Groups[1].Value)) { $properties[$m.Groups[1].Value] = $m.Groups[2].Value }
}

# Package id (case-insensitive) -> version Uno is built against, with $(Property) resolved.
$corePins = @{}
for ($i = 0; $i -lt $propsLines.Count; $i++) {
    if ($propsLines[$i] -match '<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"') {
        $id = $Matches[1]; $raw = $Matches[2]
        $version = [regex]::Replace($raw, '\$\((\w+)\)', { param($p) if ($properties.ContainsKey($p.Groups[1].Value)) { $properties[$p.Groups[1].Value] } else { $p.Value } })
        $corePins[$id.ToLowerInvariant()] = @{ Id = $id; Version = $version; Raw = $raw; Line = $i + 1 }
    }
}

foreach ($family in $Families) {
    if (-not $properties.ContainsKey($family.Property)) { throw "<$($family.Property)> not found in src/Directory.Packages.props." }
    $family.Version = $properties[$family.Property]
    Write-Host "$($family.Prefix)*: $($family.Version) (`$($($family.Property)))"
}

# --- 3a. Families use their shared property in Directory.Packages.props -----------------------
foreach ($pin in $corePins.Values) {
    $family = Get-Family $pin.Id
    if ($family) {
        $checked++
        if ($pin.Raw -ne "`$($($family.Property))") {
            $errors.Add("src/Directory.Packages.props:$($pin.Line): $($pin.Id) uses '$($pin.Raw)'; use `$($($family.Property)) so it moves with the other $($family.Prefix) packages.")
        }
    }
}

# --- 1. Nuspec floors match the build version -----------------------------------------------------
$nuspecFloors = @{}   # package id (lower) -> list of @{ Version; Where }
$nuspecs = Get-ChildItem -Path (Join-Path $RepoRoot 'build/nuget') -Filter '*.nuspec' -File -ErrorAction SilentlyContinue
foreach ($nuspec in $nuspecs) {
    $lines = Get-Content $nuspec.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($m in [regex]::Matches($lines[$i], '<dependency\s+id="([^"]+)"\s+version="([^"]+)"', 'IgnoreCase')) {
            $id = $m.Groups[1].Value
            if ($m.Groups[2].Value -like 'to-be-set*') { continue }   # Uno's own packages, versioned by CI
            $version = Get-LowerBound $m.Groups[2].Value
            $where = "$(Get-RelativePath $nuspec.FullName):$($i + 1)"
            if (-not $nuspecFloors.ContainsKey($id.ToLowerInvariant())) { $nuspecFloors[$id.ToLowerInvariant()] = [System.Collections.Generic.List[object]]::new() }
            $nuspecFloors[$id.ToLowerInvariant()].Add(@{ Version = $version; Where = $where })

            $pin = $corePins[$id.ToLowerInvariant()]
            if (-not $pin) { continue }
            $checked++
            if ($version -eq $pin.Version) { continue }
            if ($KnownFloorExceptions.ContainsKey($pin.Id) -and $KnownFloorExceptions[$pin.Id] -eq $version) { continue }
            $hint = if (Get-Family $id) { ' An app whose managed and native versions differ fails at startup.' } else { '' }
            $errors.Add("${where}: $id is $version; Uno is built against $($pin.Version) (src/Directory.Packages.props:$($pin.Line)).$hint")
        }
    }
}

# --- 2 + 3b. Uno.Sdk.Private manifest ---------------------------------------------------------
$manifestPath = Join-Path $RepoRoot 'src/Uno.Sdk/packages.json'
if (Test-Path $manifestPath) {
    $groups = Get-Content $manifestPath -Raw | ConvertFrom-Json
    foreach ($group in $groups) {
        if ($group.version -notmatch '^\d') { continue }   # placeholders such as DefaultUnoVersion
        $pins = @(@{ Name = 'version'; Value = $group.version })
        if ($group.versionOverride) {
            foreach ($o in $group.versionOverride.PSObject.Properties) { $pins += @{ Name = "$($o.Name) override"; Value = $o.Value } }
        }

        # 3b. SkiaSharp / HarfBuzzSharp groups are exactly the build version.
        $groupFamilies = @($group.packages | ForEach-Object { Get-Family $_ } | Where-Object { $_ } | Sort-Object { $_.Prefix } -Unique)
        foreach ($family in $groupFamilies) {
            foreach ($p in $pins) {
                $checked++
                if ($p.Value -ne $family.Version) {
                    $errors.Add("src/Uno.Sdk/packages.json: group '$($group.group)' $($p.Name) is $($p.Value); expected $($family.Version) (`$($($family.Property))).")
                }
            }
        }

        # 2. Every pin is at or above the nuspec floors of its packages.
        foreach ($package in $group.packages) {
            $floors = $nuspecFloors[$package.ToLowerInvariant()]
            if (-not $floors) { continue }
            foreach ($floor in $floors) {
                foreach ($p in $pins) {
                    $checked++
                    if ((Compare-PackageVersion $p.Value $floor.Version) -lt 0) {
                        $errors.Add("src/Uno.Sdk/packages.json: group '$($group.group)' $($p.Name) $($p.Value) is below the $package floor $($floor.Version) in $($floor.Where); apps would fail to restore (NU1605).")
                    }
                }
            }
        }
    }
}
else {
    Write-Host "Skipped: src/Uno.Sdk/packages.json not found."
}

# --- 3c. CI and build scripts passing the version explicitly (-p:SkiaSharpVersion=..., /p:...) ---
$self = (Resolve-Path $PSCommandPath).Path
$scripts = Get-ChildItem -Path (Join-Path $RepoRoot 'build') -Recurse -File -Include '*.yml', '*.yaml', '*.ps1', '*.sh', '*.props', '*.targets' |
    Where-Object { $_.FullName -ne $self }
foreach ($script in $scripts) {
    $lines = Get-Content $script.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($family in $Families) {
            # Uno.Sdk spells it HarfBuzzSharpVersion, Directory.Packages.props HarfbuzzSharpVersion: match both.
            foreach ($m in [regex]::Matches($lines[$i], "[-/]p:$($family.Property)=([0-9][^\s;`"']*)", 'IgnoreCase')) {
                $checked++
                if ($m.Groups[1].Value -ne $family.Version) {
                    $errors.Add("$(Get-RelativePath $script.FullName):$($i + 1): passes $($family.Property)=$($m.Groups[1].Value); expected $($family.Version).")
                }
            }
        }
    }
}

Write-Host "Checked $checked version references against src/Directory.Packages.props."

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Package versions are out of sync ($($errors.Count)):" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  - $e" -ForegroundColor Red
        if ($env:TF_BUILD) { Write-Host "##vso[task.logissue type=error]$e" }
    }
    Write-Host ""
    Write-Host "When you change a version in src/Directory.Packages.props, update the nuspecs in build/nuget and src/Uno.Sdk/packages.json in the same change." -ForegroundColor Yellow
    exit 1
}

Write-Host "All package versions are in sync." -ForegroundColor Green
