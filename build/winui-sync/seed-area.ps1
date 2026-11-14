<#
.SYNOPSIS
Proposes a manifest entry for a ported area, taking the baseline from the MUX Reference headers.
.DESCRIPTION
Prints the proposed entry and the header refs it is based on. Review them before using -Write:
a baseline that is too new silently drops upstream changes.
#>
param(
	[Parameter(Mandatory)][string]$Id,
	[Parameter(Mandatory)][string]$Title,
	[Parameter(Mandatory)][string]$UpstreamRepo,
	[Parameter(Mandatory)][string[]]$UpstreamSources,
	[string[]]$UpstreamTests = @(),
	[string]$UpstreamStrings,
	[Parameter(Mandatory)][string[]]$UnoSources,
	[string[]]$UnoTests = @(),
	[string]$UnoStrings,
	[string]$UnoRoot = (Resolve-Path "$PSScriptRoot/../..").Path,
	[string]$ManifestPath = "$PSScriptRoot/areas.json",
	# Use this commit or tag instead of the baseline derived from the headers.
	[string]$Baseline,
	# Do not add the control's theme XAML (FluentTheme copies, upstream CommonStyles) to the mapping.
	[switch]$NoThemeXaml,
	[switch]$Write)

$ErrorActionPreference = 'Stop'
Import-Module "$PSScriptRoot/WinUISync.psm1" -Force

# `pwsh -File` passes "a,b" as one string, so split lists here.
$split = { param($values) @(@($values) | ForEach-Object { "$_" -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
$UpstreamSources = @(& $split $UpstreamSources)
$UpstreamTests = @(& $split $UpstreamTests)
$UnoSources = @(& $split $UnoSources)
$UnoTests = @(& $split $UnoTests)

$latest = Get-LatestStableTag -UpstreamRepo $UpstreamRepo
if (-not $NoThemeXaml) {
	$covered = { param($path, $roots) [bool](@($roots) | Where-Object { $path -eq $_.Trim('/') -or $path.StartsWith("$($_.Trim('/'))/") }) }
	$added = @()
	foreach ($theme in Find-AreaThemeXaml -UpstreamRepo $UpstreamRepo -Ref $latest.Sha -UnoRoot $UnoRoot -UpstreamSources $UpstreamSources) {
		if (-not (& $covered $theme.Uno $UnoSources)) { $UnoSources += $theme.Uno; $added += "  uno:      $($theme.Uno)" }
		if ($theme.Upstream -and -not (& $covered $theme.Upstream $UpstreamSources)) { $UpstreamSources += $theme.Upstream; $added += "  upstream: $($theme.Upstream)" }
	}
	if ($added) {
		"Theme XAML added to the mapping (-NoThemeXaml to leave it out):"
		$added
	}
}

$scan =@(@($UnoSources) + @($UnoTests) | ForEach-Object { Join-Path $UnoRoot $_ } | Where-Object { Test-Path $_ })
$references = @(Get-MuxReferences -Paths $scan)
$result = Resolve-AreaBaseline -UpstreamRepo $UpstreamRepo -References $references

"Header refs for '$Id':"
$references | Where-Object Kind -NE 'none' | Group-Object Ref | Sort-Object Count -Descending | ForEach-Object {
	$sha = $result.Resolved[$_.Name]
	$when = if ($sha) { git -C $UpstreamRepo log -1 --format=%cs $sha } else { 'UNRESOLVED' }
	"  {0,3} file(s)  {1}  ->  {2}  {3}" -f $_.Count, $_.Name, $(if ($sha) { $sha.Substring(0, 10) } else { '-' }), $when
}
if ($result.Unresolved) { "Unresolved refs: $($result.Unresolved -join ', ')" }
if ($result.NoRef) {
	"Files with no recorded commit or tag ($($result.NoRef.Count)):"
	$result.NoRef | ForEach-Object { "  $([System.IO.Path]::GetRelativePath($UnoRoot, $_))" }
}
$chosen = $result.Baseline
if ($Baseline) {
	$chosen = git -C $UpstreamRepo rev-parse --verify --quiet "$Baseline^{commit}" 2>$null
	if ($LASTEXITCODE -ne 0 -or -not $chosen) { throw "baseline '$Baseline' is not a commit in $UpstreamRepo" }
	"Header-derived baseline would be: $($result.Baseline)"
}
if (-not $chosen) { throw "no baseline could be derived for '$Id'; pass one with -Baseline" }

$area = [pscustomobject]@{
	id       = $Id
	title    = $Title
	baseline = $chosen
	model    = $null
	upstream = [pscustomobject]@{ sources = @($UpstreamSources); tests = @($UpstreamTests); strings = $UpstreamStrings }
	uno      = [pscustomobject]@{ sources = @($UnoSources); tests = @($UnoTests); strings = $UnoStrings }
}
"Proposed baseline: $chosen ($(git -C $UpstreamRepo log -1 --format='%cs %s' $chosen))"

# A mistyped path would read as "never changes"; make sure upstream really has each one.
Get-AreaTreeDiffs -Area $area -UpstreamRepo $UpstreamRepo -TargetSha $latest.Sha | Out-Null
if ($UpstreamStrings -and -not (Resolve-UpstreamPath -UpstreamRepo $UpstreamRepo -Ref $latest.Sha -Path $UpstreamStrings)) {
	throw "upstream strings path '$UpstreamStrings' not found at $($latest.Tag)"
}

if ($Write) {
	$areas = @(Get-SyncAreas -ManifestPath $ManifestPath | Where-Object id -NE $Id) + $area
	Write-SyncAreas -ManifestPath $ManifestPath -Areas $areas
	"Written to $ManifestPath"
}
