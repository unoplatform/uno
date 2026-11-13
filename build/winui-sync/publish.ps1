<#
.SYNOPSIS
Applies one area's port result to its sync branch and creates or updates its issue and draft PR.
.DESCRIPTION
Runs without an agent. -ArtifactDir is the port job's artifact (sync.patch, sync-report.md, status.json);
it may be absent when the area only has translation changes.
#>
param(
	[Parameter(Mandatory)][string]$Id,
	[Parameter(Mandatory)][string]$TargetTag,
	[Parameter(Mandatory)][string]$TargetSha,
	[Parameter(Mandatory)][string]$MasterSha,
	[Parameter(Mandatory)][string]$UpstreamRepo,
	[string]$ArtifactDir,
	[int]$PrNumber = 0,
	[string]$CodeChanged = 'false',
	[string]$StringsChanged = 'false',
	[string]$BaseBranch = 'master',
	[string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path,
	[switch]$DryRun)

$ErrorActionPreference = 'Stop'
if (-not (Get-Module WinUISync)) { Import-Module "$PSScriptRoot/WinUISync.psm1" }

$portStatus = 'missing'
$statusFile = if ($ArtifactDir) { Join-Path $ArtifactDir 'status.json' } else { $null }
if ($statusFile -and (Test-Path -LiteralPath $statusFile)) {
	$status = Get-Content -Raw -LiteralPath $statusFile | ConvertFrom-Json
	$portStatus = if ("$($status.conflict)" -eq 'true') { 'conflict' } else { 'ok' }
}

$result = Publish-SyncArea -RepoRoot $RepoRoot -ManifestPath (Join-Path $RepoRoot 'build/winui-sync/areas.json') `
	-Id $Id -TargetTag $TargetTag -TargetSha $TargetSha -MasterSha $MasterSha -UpstreamRepo $UpstreamRepo `
	-PatchPath $(if ($ArtifactDir) { Join-Path $ArtifactDir 'sync.patch' }) `
	-ReportPath $(if ($ArtifactDir) { Join-Path $ArtifactDir 'sync-report.md' }) `
	-PrNumber $PrNumber -CodeChanged ($CodeChanged -eq 'true') -StringsChanged ($StringsChanged -eq 'true') `
	-PortStatus $portStatus -BaseBranch $BaseBranch -DryRun $DryRun.IsPresent

if ($DryRun) {
	"=== ISSUE: $($result.IssueTitle)"
	$result.IssueBody
	"=== PR: $($result.PrTitle)"
	$result.PrBody
}
"outcome=$($result.Outcome)"
