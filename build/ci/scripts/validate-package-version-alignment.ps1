#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fails when a package version written by hand drifts from the version Uno is built against.

.DESCRIPTION
    Several files repeat the versions of Uno's dependencies, and a version bump must change all of
    them together. The source of truth (-Source) holds the versions Uno is built against. Each entry
    of -Files says how to read one or more other files (Kind):

      - NuspecFloor: dependency floors of the packed Uno packages (build/nuget/*.nuspec). An app
        resolves these floors, so a floor below the build version gives the app an older package
        than the one Uno was compiled against, and .NET fails to load it at runtime.
      - SdkManifest: Uno.Sdk manifests (packages.json), which pin the packages the Uno.Sdk
        references directly. A pin below a nuspec floor fails the app's restore (NU1605).
      - MSBuildArgs: CI scripts passing -p:SkiaSharpVersion=... (or /p:, -property:) explicitly.

    Checks:
      1. Every nuspec dependency on a package in -Source declares that same version (except the
         -FloorExceptions).
      2. Every manifest pin is at or above the nuspec floor of the same package. A floor in a
         <group targetFramework="net11.0..."> is compared with the manifest net11.0 override when
         there is one, otherwise with the base version.
      3. SkiaSharp and HarfBuzzSharp match exactly everywhere: -Source uses the shared property,
         and the nuspecs, the manifests and CI scripts use its value. SkiaSharp checks its native
         library at startup, and an app whose managed SkiaSharp and native libSkiaSharp differ fails
         with "The version of the native libSkiaSharp library (...) is incompatible with this
         version of SkiaSharp".

    Unlike validate-packages-json.ps1, this check needs no network and fails on every branch.

.PARAMETER RepoRoot
    Repository root. Defaults to three levels above this script.

.PARAMETER Source
    The file holding the versions Uno is built against, relative to RepoRoot.

.PARAMETER Files
    The files to check against Source: an array of @{ Path = '<path>'; Kind = '<kind>' }, where Path
    is relative to RepoRoot ('dir/file', 'dir/*.ext' or 'dir/**/*.ext') and Kind is NuspecFloor,
    SdkManifest or MSBuildArgs. Defaults to the files of the uno repository. A Path that matches no
    file fails the check, except an MSBuildArgs wildcard (a repository may have no script of a type).

.PARAMETER FloorExceptions
    Nuspec floors allowed below the build version: @{ '<package id>' = '<allowed floor>' }. Keep it
    empty when possible: .NET binds by assembly version, so an app that resolves a floor below the
    version Uno is built against fails to load it, even when the API it uses exists (see
    https://github.com/unoplatform/uno/issues/26332). An entry needs a tracking issue.
#>
param(
    [string]$RepoRoot = (Join-Path $PSScriptRoot '../../..'),
    [string]$Source = 'src/Directory.Packages.props',
    # Default for local runs. Keep in sync with the list in build/ci/setup/.azure-devops-setup-validate-packages.yml.
    [object[]]$Files = @(
        @{ Path = 'build/nuget/*.nuspec';      Kind = 'NuspecFloor' }
        @{ Path = 'src/Uno.Sdk/packages.json'; Kind = 'SdkManifest' }
        @{ Path = 'build/**/*.yml';            Kind = 'MSBuildArgs' }
        @{ Path = 'build/**/*.ps1';            Kind = 'MSBuildArgs' }
        @{ Path = 'build/**/*.sh';             Kind = 'MSBuildArgs' }
        @{ Path = 'build/**/*.props';          Kind = 'MSBuildArgs' }
        @{ Path = 'build/**/*.targets';        Kind = 'MSBuildArgs' }
    ),
    [hashtable]$FloorExceptions = @{}
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path

# Package families whose managed and native packages must have the exact same version everywhere.
# Prefix = package id prefix; Property = the -Source property holding the version.
# Note: PowerShell variable names are case-insensitive, so no other variable may be named $families.
$Families = @(
    @{ Prefix = 'SkiaSharp';     Property = 'SkiaSharpVersion' }
    @{ Prefix = 'HarfBuzzSharp'; Property = 'HarfbuzzSharpVersion' }
)

$Kinds = @('NuspecFloor', 'SdkManifest', 'MSBuildArgs')

$errors = [System.Collections.Generic.List[string]]::new()
$checked = 0

function Get-RelativePath([string]$Path) {
    [System.IO.Path]::GetRelativePath($RepoRoot, $Path).Replace('\', '/')
}

# Resolves 'dir/file', 'dir/*.ext' or 'dir/**/*.ext' (relative to RepoRoot) to files.
function Resolve-FilePattern([string]$Pattern) {
    $segments = $Pattern.Replace('\', '/').Trim('/') -split '/'
    $first = -1
    for ($i = 0; $i -lt $segments.Count; $i++) { if ($segments[$i] -match '[*?]') { $first = $i; break } }
    if ($first -lt 0) {
        $file = Join-Path $RepoRoot ($segments -join '/')
        return @(if (Test-Path $file -PathType Leaf) { Get-Item $file })
    }
    $base = if ($first -eq 0) { $RepoRoot } else { Join-Path $RepoRoot ($segments[0..($first - 1)] -join '/') }
    $rest = $segments[$first..($segments.Count - 1)]
    $leaf = $rest[-1]
    if ($rest.Count -eq 2 -and $rest[0] -eq '**') { $recurse = $true }
    elseif ($rest.Count -eq 1) { $recurse = $false }
    else { throw "Unsupported path '$Pattern' in -Files: use 'dir/file', 'dir/*.ext' or 'dir/**/*.ext'." }
    if (-not (Test-Path $base -PathType Container)) { return @() }
    # -like is case-insensitive on every OS, unlike -Filter.
    @(Get-ChildItem -Path $base -File -Recurse:$recurse | Where-Object { $_.Name -like $leaf })
}

# A nuspec dependency version can be a range ("[1.0]", "[1.0,2.0)"): use its lower bound.
function Get-LowerBound([string]$Version) {
    $v = $Version.Trim().TrimStart('[', '(')
    ($v -split ',')[0].TrimEnd(']', ')').Trim()
}

# NuGet version order: numeric parts first, then a release sorts above its prereleases, and
# prerelease labels compare part by part (numbers numerically, below text). Build metadata
# ("+abc") is ignored, as NuGet does.
function Compare-PackageVersion([string]$A, [string]$B, [string]$Context) {
    if ("$A" -notmatch '^\d+(\.\d+)*(-[0-9A-Za-z.-]+)?(\+.*)?$' -or "$B" -notmatch '^\d+(\.\d+)*(-[0-9A-Za-z.-]+)?(\+.*)?$') {
        throw "Can't compare package versions '$A' and '$B' ($Context): not a NuGet version."
    }
    $pa = ($A -split '\+', 2)[0] -split '-', 2; $pb = ($B -split '\+', 2)[0] -split '-', 2
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

# Blanks out XML comments but keeps their line breaks, so line numbers and offsets stay right.
function Remove-XmlComments([string]$Text) {
    [regex]::Replace($Text, '<!--.*?-->', { param($c) [regex]::Replace($c.Value, '[^\n]', ' ') }, 'Singleline')
}

# Finds the <Element ...> tags in an XML text, whatever the attribute order or line breaks, and
# skips commented-out ones. Returns @{ Attributes (lower-case names); Line; Index; Text } per tag.
function Get-XmlElements([string]$Text, [string]$Element) {
    $clean = Remove-XmlComments $Text
    $lineStarts = [System.Collections.Generic.List[int]]::new(); $lineStarts.Add(0)
    for ($i = 0; $i -lt $clean.Length; $i++) { if ($clean[$i] -eq "`n") { $lineStarts.Add($i + 1) } }
    foreach ($m in [regex]::Matches($clean, "<$Element\b([^>]*)>", 'IgnoreCase')) {
        $attributes = @{}
        foreach ($a in [regex]::Matches($m.Groups[1].Value, '([\w.:-]+)\s*=\s*"([^"]*)"')) {
            $attributes[$a.Groups[1].Value.ToLowerInvariant()] = $a.Groups[2].Value
        }
        $line = $lineStarts.BinarySearch($m.Index); if ($line -lt 0) { $line = (-bnot $line) - 1 }
        @{ Attributes = $attributes; Line = $line + 1; Index = $m.Index; Text = ($m.Value -replace '\s+', ' ') }
    }
}

# --- Files to check ------------------------------------------------------------------------------
$filesByKind = @{}
foreach ($k in $Kinds) { $filesByKind[$k] = [System.Collections.Generic.List[System.IO.FileInfo]]::new() }
foreach ($entry in $Files) {
    $path = $entry.Path; $kind = $entry.Kind
    if (-not $path -or -not $kind) { throw "Each -Files entry needs a Path and a Kind: @{ Path = 'dir/*.ext'; Kind = 'NuspecFloor' }." }
    if ($Kinds -notcontains $kind) { throw "Unknown Kind '$kind' for '$path' in -Files: use $($Kinds -join ', ')." }
    $found = Resolve-FilePattern $path
    if ($found.Count -eq 0) {
        # A repository may have no script of a given type, but a missing nuspec, manifest or named
        # file is most likely a typo in -Files, which would leave versions unchecked.
        if ($kind -eq 'MSBuildArgs' -and $path -match '[*?]') { Write-Host "Note: '$path' ($kind) matches no file."; continue }
        $errors.Add("-Files: '$path' ($kind) matches no file.")
        continue
    }
    foreach ($f in $found) {
        if (-not ($filesByKind[$kind] | Where-Object { $_.FullName -eq $f.FullName })) { $filesByKind[$kind].Add($f) }
    }
}

# --- Source of truth --------------------------------------------------------------------------------
$propsPath = Join-Path $RepoRoot $Source
if (-not (Test-Path $propsPath -PathType Leaf)) { throw "-Source not found: $propsPath" }
$sourceName = Get-RelativePath $propsPath
$propsText = (Get-Content $propsPath -Raw) -replace "`r`n", "`n"

$properties = @{}
foreach ($m in [regex]::Matches((Remove-XmlComments $propsText), '<(\w+)>\s*([^<\s]+)\s*</\1>')) {
    if (-not $properties.ContainsKey($m.Groups[1].Value)) { $properties[$m.Groups[1].Value] = $m.Groups[2].Value }
}
function Resolve-Properties([string]$Value) {
    [regex]::Replace($Value, '\$\((\w+)\)', { param($p) if ($properties.ContainsKey($p.Groups[1].Value)) { $properties[$p.Groups[1].Value] } else { $p.Value } })
}

# Package id (lower-case) -> @{ Id; Version; Raw; Line; Overrides }. Version is what Uno is built
# against, with $(Property) resolved. Overrides are the conditional <PackageVersion Update="..."> items.
$corePins = @{}
$updates = [System.Collections.Generic.List[object]]::new()
foreach ($e in Get-XmlElements $propsText 'PackageVersion') {
    $isInclude = $e.Attributes.ContainsKey('include')
    $id = if ($isInclude) { $e.Attributes['include'] } else { $e.Attributes['update'] }
    $raw = $e.Attributes['version']
    if (-not $id -or -not $raw) {
        # Fail loudly rather than leave a pin unchecked.
        $errors.Add("${sourceName}:$($e.Line): can't read the package id and Version of $($e.Text). Write it as <PackageVersion Include=`"Id`" Version=`"x`" />.")
        continue
    }
    $pin = @{ Id = $id; Version = (Resolve-Properties $raw); Raw = $raw; Line = $e.Line; Overrides = [System.Collections.Generic.List[object]]::new() }
    if ($isInclude -and -not $corePins.ContainsKey($id.ToLowerInvariant())) { $corePins[$id.ToLowerInvariant()] = $pin }
    else { $updates.Add($pin) }
}
foreach ($u in $updates) {
    $base = $corePins[$u.Id.ToLowerInvariant()]
    if ($base) { $base.Overrides.Add($u) }
}

foreach ($family in $Families) {
    if (-not $properties.ContainsKey($family.Property)) { throw "<$($family.Property)> not found in $sourceName." }
    $family.Version = $properties[$family.Property]
    Write-Host "$($family.Prefix)*: $($family.Version) (`$($($family.Property)))"
}

# --- 3a. Families use their shared property in the source of truth ------------------------------
foreach ($pin in @($corePins.Values) + @($updates)) {
    $family = Get-Family $pin.Id
    if ($family) {
        $checked++
        if ($pin.Raw -ne "`$($($family.Property))") {
            $errors.Add("${sourceName}:$($pin.Line): $($pin.Id) uses '$($pin.Raw)'; use `$($($family.Property)) so it moves with the other $($family.Prefix) packages.")
        }
    }
}

# --- 1. Nuspec floors match the build version (Kind NuspecFloor) -------------------------------------
$nuspecFloors = @{}   # package id (lower) -> list of @{ Version; Where; Tfm }
foreach ($nuspec in $filesByKind['NuspecFloor']) {
    $text = (Get-Content $nuspec.FullName -Raw) -replace "`r`n", "`n"
    # <group targetFramework="..."> spans, so each floor knows the framework it applies to.
    $tfmGroups = @([regex]::Matches((Remove-XmlComments $text), '<group\b([^>]*)>(.*?)</group>', 'Singleline, IgnoreCase') | ForEach-Object {
        @{ Start = $_.Index; End = $_.Index + $_.Length; Tfm = [regex]::Match($_.Groups[1].Value, 'targetFramework\s*=\s*"([^"]*)"', 'IgnoreCase').Groups[1].Value }
    })
    foreach ($e in Get-XmlElements $text 'dependency') {
        $where = "$(Get-RelativePath $nuspec.FullName):$($e.Line)"
        $id = $e.Attributes['id']
        if (-not $id) { $errors.Add("${where}: can't read the id of $($e.Text)."); continue }
        $rawVersion = $e.Attributes['version']
        if (-not $rawVersion -or $rawVersion -like 'to-be-set*') { continue }   # no floor, or Uno's own packages versioned by CI
        $version = Get-LowerBound $rawVersion
        $tfm = ($tfmGroups | Where-Object { $e.Index -ge $_.Start -and $e.Index -lt $_.End } | Select-Object -First 1).Tfm
        if (-not $nuspecFloors.ContainsKey($id.ToLowerInvariant())) { $nuspecFloors[$id.ToLowerInvariant()] = [System.Collections.Generic.List[object]]::new() }
        $nuspecFloors[$id.ToLowerInvariant()].Add(@{ Version = $version; Where = $where; Tfm = $tfm })

        $pin = $corePins[$id.ToLowerInvariant()]
        if (-not $pin) { continue }
        $checked++
        $allowed = @($pin.Version) + @($pin.Overrides | ForEach-Object { $_.Version })
        if ($allowed -contains $version) { continue }
        if ($FloorExceptions.ContainsKey($pin.Id) -and $FloorExceptions[$pin.Id] -eq $version) { continue }
        $hint = if (Get-Family $id) { ' An app whose managed and native versions differ fails at startup.' } else { '' }
        $errors.Add("${where}: $id is $version; Uno is built against $($pin.Version) (${sourceName}:$($pin.Line)).$hint")
    }
}

# --- 2 + 3b. Uno.Sdk manifests (Kind SdkManifest) --------------------------------------------------
foreach ($manifest in $filesByKind['SdkManifest']) {
    $manifestName = Get-RelativePath $manifest.FullName
    $groups = Get-Content $manifest.FullName -Raw | ConvertFrom-Json
    foreach ($group in $groups) {
        if ($group.version -notmatch '^\d') { continue }   # placeholders such as DefaultUnoVersion
        $pins = @(@{ Name = 'version'; Value = $group.version; Tfm = $null })
        if ($group.versionOverride) {
            foreach ($o in $group.versionOverride.PSObject.Properties) { $pins += @{ Name = "$($o.Name) override"; Value = $o.Value; Tfm = $o.Name.ToLowerInvariant() } }
        }

        # 3b. SkiaSharp / HarfBuzzSharp groups are exactly the build version.
        $groupFamilies = @($group.packages | ForEach-Object { Get-Family $_ } | Where-Object { $_ } | Sort-Object { $_.Prefix } -Unique)
        foreach ($family in $groupFamilies) {
            foreach ($p in $pins) {
                $checked++
                if ($p.Value -ne $family.Version) {
                    $errors.Add("${manifestName}: group '$($group.group)' $($p.Name) is $($p.Value); expected $($family.Version) (`$($($family.Property)) in $sourceName).")
                }
            }
        }

        # 2. Every pin is at or above the nuspec floors of its packages. A floor in a
        #    <group targetFramework="net11.0-..."> is compared with the net11.0 override when the group
        #    has one, otherwise with the base version; a floor outside any group, with every pin.
        foreach ($package in $group.packages) {
            $floors = $nuspecFloors[$package.ToLowerInvariant()]
            if (-not $floors) { continue }
            foreach ($floor in $floors) {
                $applicable = $pins
                if ($floor.Tfm) {
                    $floorTfm = ($floor.Tfm -split '-')[0].ToLowerInvariant()
                    $override = $pins | Where-Object { $_.Tfm -eq $floorTfm } | Select-Object -First 1
                    $applicable = if ($override) { @($override) } else { @($pins[0]) }
                }
                foreach ($p in $applicable) {
                    $checked++
                    if ((Compare-PackageVersion $p.Value $floor.Version "$manifestName group '$($group.group)' vs $($floor.Where)") -lt 0) {
                        $errors.Add("${manifestName}: group '$($group.group)' $($p.Name) $($p.Value) is below the $package floor $($floor.Version) in $($floor.Where); apps would fail to restore (NU1605).")
                    }
                }
            }
        }
    }
}

# --- 3c. CI and build scripts passing the version explicitly (Kind MSBuildArgs) --------------------
# This script matches build/**/*.ps1 too: skip it, so an example of a -p:SkiaSharpVersion=<version>
# argument added to its own comments or messages later can never be reported as a drift.
$self = (Resolve-Path $PSCommandPath).Path
foreach ($script in ($filesByKind['MSBuildArgs'] | Where-Object { $_.FullName -ne $self })) {
    $lines = Get-Content $script.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($family in $Families) {
            # Uno.Sdk spells it HarfBuzzSharpVersion, Directory.Packages.props HarfbuzzSharpVersion: match both.
            foreach ($m in [regex]::Matches($lines[$i], "(?:--?|/)(?:p|property):$($family.Property)=([0-9][^\s;`"']*)", 'IgnoreCase')) {
                $checked++
                if ($m.Groups[1].Value -ne $family.Version) {
                    $errors.Add("$(Get-RelativePath $script.FullName):$($i + 1): passes $($family.Property)=$($m.Groups[1].Value); expected $($family.Version).")
                }
            }
        }
    }
}

$counts = ($Kinds | ForEach-Object { "$($filesByKind[$_].Count) $_" }) -join ', '
Write-Host "Checked $checked version references against $sourceName ($counts files)."

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Package versions are out of sync ($($errors.Count)):" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  - $e" -ForegroundColor Red
        if ($env:TF_BUILD) { Write-Host "##vso[task.logissue type=error]$e" }
    }
    Write-Host ""
    Write-Host "When you change a version in $sourceName, update the nuspecs and the Uno.Sdk manifests in the same change." -ForegroundColor Yellow
    exit 1
}

Write-Host "All package versions are in sync." -ForegroundColor Green
