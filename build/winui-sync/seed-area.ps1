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
	[switch]$Write)

$ErrorActionPreference = 'Stop'
Import-Module "$PSScriptRoot/WinUISync.psm1" -Force

$scan = @(@($UnoSources) + @($UnoTests) | ForEach-Object { Join-Path $UnoRoot $_ } | Where-Object { Test-Path $_ })
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
if (-not $result.Baseline) { throw "no baseline could be derived for '$Id'; settle one by hand" }

$area = [pscustomobject]@{
	id       = $Id
	title    = $Title
	baseline = $result.Baseline
	model    = $null
	upstream = [pscustomobject]@{ sources = @($UpstreamSources); tests = @($UpstreamTests); strings = $UpstreamStrings }
	uno      = [pscustomobject]@{ sources = @($UnoSources); tests = @($UnoTests); strings = $UnoStrings }
}
"Proposed baseline: $($result.Baseline) ($(git -C $UpstreamRepo log -1 --format='%cs %s' $result.Baseline))"

if ($Write) {
	$areas = @(Get-SyncAreas -ManifestPath $ManifestPath | Where-Object id -NE $Id) + $area
	Write-SyncAreas -ManifestPath $ManifestPath -Areas $areas
	"Written to $ManifestPath"
}
