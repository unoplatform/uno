<#
.SYNOPSIS
Works out which tracked areas changed upstream and emits the job matrix. Uses git only - no agent.
#>
param(
	[Parameter(Mandatory)][string]$UpstreamRepo,
	[string]$UnoRoot = (Resolve-Path "$PSScriptRoot/../..").Path,
	[string]$ManifestPath = "$PSScriptRoot/areas.json",
	[string]$AreaFilter,
	[string]$TargetTag,
	[int]$MaxAreas = 10)

$ErrorActionPreference = 'Stop'
if (-not (Get-Module WinUISync)) { Import-Module "$PSScriptRoot/WinUISync.psm1" }

$plan = Get-SyncPlan -UpstreamRepo $UpstreamRepo -UnoRoot $UnoRoot -ManifestPath $ManifestPath -AreaFilter $AreaFilter -TargetTag $TargetTag -MaxAreas $MaxAreas

$matrixJson = ConvertTo-Json -InputObject @($plan.Matrix) -Compress -Depth 4
$outputs = @(
	"matrix=$matrixJson"
	"has_work=$(($plan.Matrix.Count -gt 0).ToString().ToLowerInvariant())"
	"target_tag=$($plan.TargetTag)"
	"target_sha=$($plan.TargetSha)"
	"master_sha=$($plan.MasterSha)"
	"has_errors=$((@($plan.Errors).Count -gt 0).ToString().ToLowerInvariant())")

$summary = @(
	"## WinUI port sync: $($plan.TargetTag)"
	''
	'| Area | Baseline | Status | Upstream diff | Open PR |'
	'|------|----------|--------|---------------|---------|')
foreach ($row in $plan.Rows) {
	$pr = if ($row.prNumber) { "#$($row.prNumber)" } else { '' }
	$summary += "| $($row.id) | ``$($row.baseline.Substring(0, 10))`` | $($row.status) | $($row.diffStat) | $pr |"
}
if ($plan.Rows.Count -eq 0) { $summary += '| _no areas matched_ | | | | |' }

if ($env:GITHUB_OUTPUT) { Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value $outputs }
if ($env:GITHUB_STEP_SUMMARY) { Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary }
$outputs
$summary
foreach ($row in $plan.Rows | Where-Object { $_.status -like 'error:*' }) { "::error title=WinUI sync area '$($row.id)'::$($row.status)" }
