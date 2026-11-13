<#
.SYNOPSIS
Puts the working tree on an area's sync branch, merged with the default branch. Never pushes.
#>
param(
	[Parameter(Mandatory)][string]$Id,
	[Parameter(Mandatory)][string]$MasterSha,
	[int]$PrNumber = 0,
	[string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)

$ErrorActionPreference = 'Stop'
if (-not (Get-Module WinUISync)) { Import-Module "$PSScriptRoot/WinUISync.psm1" }

$state = Initialize-SyncBranch -RepoRoot $RepoRoot -Id $Id -MasterSha $MasterSha -HasOpenPr:($PrNumber -gt 0)
$conflict = ($state -eq 'conflict').ToString().ToLowerInvariant()
if ($env:GITHUB_OUTPUT) { Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "conflict=$conflict" }
"conflict=$conflict"
