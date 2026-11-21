<#
.SYNOPSIS
    Checks that every package group in src/Uno.Sdk/packages.json names the MSBuild property
    that overrides its version, and that this matches what the Uno.Sdk code actually does.

.DESCRIPTION
    Each group in packages.json that a project can override (for example
    <UnoCSharpMarkupVersion>7.1.0-dev.5</UnoCSharpMarkupVersion>) carries a "versionProperty"
    field with that property name. Tools outside this repository read the field from the
    published Uno.Sdk / Uno.Sdk.Private package to know which packages a property controls,
    without keeping their own copy of the list (the canary NuGet updater does this).

    The pairing itself lives in code, so this script keeps the two in sync. It fails when:
      - a property is passed to ImplicitPackagesResolver in Uno.Implicit.Packages.targets
        but no UpdateManifest(PackageManifest.Group.X, Property) call uses it, or the reverse;
      - an UpdateManifest(Group.X, Property) call has no matching "versionProperty" on
        group X in packages.json;
      - a group's "versionProperty" doesn't match any UpdateManifest call;
      - a property is missing from Sdk.props.buildschema.json (IDE completion) or from
        doc/articles/features/using-the-uno-sdk.md (the documented list);
      - the "Implicit Packages" tables of that page list a property that isn't an Uno.Sdk
        property anymore, or link a property to a package it doesn't control.

    No network access; runs on every branch.

.PARAMETER RepoRoot
    Repository root. Defaults to two levels above this script's folder.
#>
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..' '..')).Path
}

$resolverPath = Join-Path $RepoRoot 'src/Uno.Sdk/Tasks/ImplicitPackagesResolver.cs'
$targetsPath = Join-Path $RepoRoot 'src/Uno.Sdk/targets/Uno.Implicit.Packages.targets'
$manifestPath = Join-Path $RepoRoot 'src/Uno.Sdk/packages.json'
$schemaPath = Join-Path $RepoRoot 'src/Uno.Sdk/Sdk/Sdk.props.buildschema.json'
$docPath = Join-Path $RepoRoot 'doc/articles/features/using-the-uno-sdk.md'

foreach ($path in @($resolverPath, $targetsPath, $manifestPath, $schemaPath, $docPath)) {
    if (-not (Test-Path $path)) {
        Write-Error "File not found: $path"
        exit 1
    }
}

$errors = [System.Collections.Generic.List[string]]::new()

# 1. Group -> property pairs from the code. A property may feed several groups
#    (MauiVersion moves both Maui and MauiCompatibility). Named arguments and a missing
#    "PackageManifest." prefix are accepted.
$resolver = Get-Content $resolverPath -Raw
$pairPattern = 'UpdateManifest\(\s*(?:groupName\s*:\s*)?(?:PackageManifest\.)?Group\.(\w+)\s*,\s*(?:version\s*:\s*)?(\w+)\s*\)'
$pairs = [regex]::Matches($resolver, $pairPattern) |
    ForEach-Object { [pscustomobject]@{ Group = $_.Groups[1].Value; Property = $_.Groups[2].Value } }

if (-not $pairs) {
    Write-Error "No UpdateManifest(PackageManifest.Group.X, Property) calls found in $resolverPath. Update this script if the code changed shape."
    exit 1
}

# Fail closed: every UpdateManifest call must be one this script can read, so a call written
# another way (an expression instead of a property, a variable group name...) is reported
# instead of being skipped.
$allCalls = [regex]::Matches($resolver, 'UpdateManifest\(')
if ($allCalls.Count -ne $pairs.Count) {
    $readable = [regex]::Matches($resolver, $pairPattern) | ForEach-Object { $_.Index }
    $allCalls | Where-Object { $_.Index -notin $readable } | ForEach-Object {
        $line = ($resolver.Substring(0, $_.Index) -split "`n").Count
        $text = ($resolver.Substring($_.Index) -split "`n")[0].Trim()
        $errors.Add("ImplicitPackagesResolver.cs line ${line}: this script can't read '$text'. Use UpdateManifest(PackageManifest.Group.X, XxxVersion), or update the script.")
    }
}

$codeProperties = $pairs.Property | Sort-Object -Unique

# 2. Properties the targets pass to the resolver task.
$targets = Get-Content $targetsPath -Raw
$targetProperties = [regex]::Matches($targets, '\b(\w+Version)="\$\(\1\)"') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

foreach ($property in $targetProperties) {
    if ($property -notin $codeProperties) {
        $errors.Add("$property is passed to ImplicitPackagesResolver in Uno.Implicit.Packages.targets, but no UpdateManifest(PackageManifest.Group.X, $property) call uses it.")
    }
}
foreach ($property in $codeProperties) {
    if ($property -notin $targetProperties) {
        $errors.Add("$property is used in an UpdateManifest call, but Uno.Implicit.Packages.targets doesn't pass it to ImplicitPackagesResolver.")
    }
}

# 3. Each pair must be recorded on its group in packages.json, and nothing else may be.
$groups = Get-Content $manifestPath -Raw | ConvertFrom-Json

foreach ($pair in $pairs) {
    $group = $groups | Where-Object { $_.group -ieq $pair.Group } | Select-Object -First 1
    if (-not $group) {
        $errors.Add("UpdateManifest(PackageManifest.Group.$($pair.Group), $($pair.Property)): packages.json has no group named '$($pair.Group)'.")
        continue
    }

    $declared = $group.PSObject.Properties['versionProperty']?.Value
    if (-not $declared) {
        $errors.Add("packages.json group '$($group.group)' has no ""versionProperty"". Add ""versionProperty"": ""$($pair.Property)"" as its last field.")
    }
    elseif ($declared -cne $pair.Property) {
        $errors.Add("packages.json group '$($group.group)' has ""versionProperty"": ""$declared"", but the code overrides it with $($pair.Property).")
    }
}

foreach ($group in $groups) {
    $declared = $group.PSObject.Properties['versionProperty']?.Value
    if (-not $declared) {
        continue
    }

    $match = $pairs | Where-Object { $_.Group -ieq $group.group -and $_.Property -ceq $declared }
    if (-not $match) {
        $errors.Add("packages.json group '$($group.group)' has ""versionProperty"": ""$declared"", but no UpdateManifest(PackageManifest.Group.$($group.group), $declared) call exists.")
    }
}

# 4. Every property is described for IDE completion and in the docs.
$schema = Get-Content $schemaPath -Raw
$doc = Get-Content $docPath -Raw
foreach ($property in $codeProperties) {
    if ($schema -notmatch ('"' + [regex]::Escape($property) + '"\s*:')) {
        $errors.Add("$property is missing from src/Uno.Sdk/Sdk/Sdk.props.buildschema.json (IDE completion for the Uno.Sdk properties).")
    }
    if ($doc -notmatch ('\b' + [regex]::Escape($property) + '\b')) {
        $errors.Add("$property is missing from doc/articles/features/using-the-uno-sdk.md (the documented Uno.Sdk properties).")
    }
}

# 5. The other way around: the "Implicit Packages" tables of the docs only list current Uno.Sdk
#    properties, each with a package of its group. A row left behind after a property is removed,
#    or a link to a package the property doesn't control, fails here.
$section = [regex]::Match($doc, '(?ms)^## Implicit Packages\s*$(.*?)^## Using Central Package Management')
if (-not $section.Success) {
    $errors.Add("doc/articles/features/using-the-uno-sdk.md: the '## Implicit Packages' section (up to '## Using Central Package Management') was not found. Update this script if the page was reorganized.")
}
else {
    $packagesByProperty = @{}
    foreach ($group in $groups) {
        $declared = $group.PSObject.Properties['versionProperty']?.Value
        if ($declared) {
            if (-not $packagesByProperty.ContainsKey($declared)) {
                $packagesByProperty[$declared] = [System.Collections.Generic.List[string]]::new()
            }
            $group.packages | ForEach-Object { $packagesByProperty[$declared].Add($_) }
        }
    }

    $rows = [regex]::Matches($section.Groups[1].Value, '(?m)^\|\s*`(\w+Version)`\s*\|([^|]*)\|')
    foreach ($row in $rows) {
        $property = $row.Groups[1].Value
        $cell = $row.Groups[2].Value

        if (-not $packagesByProperty.ContainsKey($property)) {
            $errors.Add("doc/articles/features/using-the-uno-sdk.md lists $property in 'Implicit Packages', but it isn't an Uno.Sdk version property (removed or renamed?).")
            continue
        }

        $packages = $packagesByProperty[$property]
        # Exact package links (nuget.org/packages/<id>) and search links (nuget.org/packages?q=<prefix>).
        $linked = [regex]::Matches($cell, 'nuget\.org/packages/([A-Za-z0-9_.\-]+)') | ForEach-Object { $_.Groups[1].Value }
        $searched = [regex]::Matches($cell, 'nuget\.org/packages\?q=([A-Za-z0-9_.\-]+)') | ForEach-Object { $_.Groups[1].Value }

        $found = @($linked | Where-Object { $_ -in $packages }) + @($searched | Where-Object { $prefix = $_; $packages | Where-Object { $_ -like "$prefix*" } })
        if (($linked -or $searched) -and $found.Count -eq 0) {
            $errors.Add("doc/articles/features/using-the-uno-sdk.md: the $property row links to $(($linked + $searched) -join ', '), but the property controls $($packages -join ', ').")
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Uno.Sdk version properties are out of sync:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Uno.Sdk version properties are in sync: $($pairs.Count) group/property pairs, $($codeProperties.Count) properties." -ForegroundColor Green
