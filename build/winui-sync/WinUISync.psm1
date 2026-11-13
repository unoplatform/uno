Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:UpstreamSlug = 'microsoft/microsoft-ui-xaml'
$script:StableTagPattern = '^winui3/release/(\d+\.\d+\.\d+)$'

#region git

# Runs git and returns stdout lines. Throws on a non-zero exit code unless it is listed in -AllowExitCodes.
function Invoke-Git {
	param(
		[Parameter(Mandatory, Position = 0)][string]$Repo,
		[Parameter(Mandatory, Position = 1)][string[]]$GitArgs,
		[int[]]$AllowExitCodes = @())
	$all = & git -C $Repo @GitArgs 2>&1
	$code = $LASTEXITCODE
	$script:LastGitExitCode = $code
	if ($code -ne 0 -and $AllowExitCodes -notcontains $code) {
		throw "git $($GitArgs -join ' ') failed ($code): $($all -join "`n")"
	}
	$all | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" }
}

function Get-LastGitExitCode { $script:LastGitExitCode }

#endregion

#region tags and manifest

function Get-LatestStableTag {
	param([Parameter(Mandatory)][string]$UpstreamRepo)
	$stable = @(Invoke-Git $UpstreamRepo @('tag', '-l', 'winui3/release/*') |
			Where-Object { $_ -match $script:StableTagPattern } |
			ForEach-Object { [pscustomobject]@{ Tag = $_; Version = [version]($_ -replace $script:StableTagPattern, '$1') } } |
			Sort-Object Version)
	if ($stable.Count -eq 0) { throw "no stable winui3/release/X.Y.Z tag found in $UpstreamRepo" }
	$latest = $stable[-1]
	[pscustomobject]@{
		Tag     = $latest.Tag
		Version = $latest.Version.ToString()
		Sha     = (Invoke-Git $UpstreamRepo @('rev-list', '-n1', $latest.Tag))
	}
}

function Get-OptionalProperty {
	param($Object, [string]$Name)
	if ($null -ne $Object -and $Object.PSObject.Properties[$Name]) { $Object.$Name } else { $null }
}

function ConvertTo-SyncArea {
	param($Raw)
	$id = Get-OptionalProperty $Raw 'id'
	if (-not $id) { throw 'manifest entry is missing id' }
	$baseline = Get-OptionalProperty $Raw 'baseline'
	if ("$baseline" -notmatch '^[0-9a-f]{40}$') { throw "area '$id': baseline must be a full 40-character commit SHA" }
	$upstream = Get-OptionalProperty $Raw 'upstream'
	$uno = Get-OptionalProperty $Raw 'uno'
	$sides = [ordered]@{}
	foreach ($side in @(@{ Name = 'upstream'; Value = $upstream }, @{ Name = 'uno'; Value = $uno })) {
		$sources = @(Get-OptionalProperty $side.Value 'sources' | Where-Object { $_ })
		if ($sources.Count -eq 0) { throw "area '$id': $($side.Name).sources must list at least one path" }
		$sides[$side.Name] = [pscustomobject][ordered]@{
			sources = $sources
			tests   = @(Get-OptionalProperty $side.Value 'tests' | Where-Object { $_ })
			strings = (Get-OptionalProperty $side.Value 'strings')
		}
	}
	$title = Get-OptionalProperty $Raw 'title'
	[pscustomobject][ordered]@{
		id       = [string]$id
		title    = if ($title) { [string]$title } else { [string]$id }
		baseline = [string]$baseline
		model    = (Get-OptionalProperty $Raw 'model')
		upstream = $sides['upstream']
		uno      = $sides['uno']
	}
}

function Get-SyncAreas {
	param([Parameter(Mandatory, Position = 0)][string]$ManifestPath)
	Get-SyncAreasFromJson -Json (Get-Content -Raw -LiteralPath $ManifestPath)
}

function Get-SyncAreasFromJson {
	param([Parameter(Mandatory)][string]$Json)
	$doc = $Json | ConvertFrom-Json
	$areas = @(@(Get-OptionalProperty $doc 'areas') | Where-Object { $null -ne $_ } | ForEach-Object { ConvertTo-SyncArea $_ })
	$dupes = @($areas | Group-Object id | Where-Object Count -GT 1)
	if ($dupes.Count -gt 0) { throw "duplicate area id: $($dupes.Name -join ', ')" }
	$areas
}

# The manifest is always written in this one canonical shape, so diffs stay minimal.
function Write-SyncAreas {
	param([Parameter(Mandatory)][string]$ManifestPath, [object[]]$Areas)
	$out = foreach ($a in @($Areas)) {
		$entry = [ordered]@{ id = $a.id; title = $a.title; baseline = $a.baseline }
		if ($a.model) { $entry.model = $a.model }
		foreach ($side in 'upstream', 'uno') {
			$s = [ordered]@{ sources = @($a.$side.sources) }
			if (@($a.$side.tests).Count -gt 0) { $s.tests = @($a.$side.tests) }
			if ($a.$side.strings) { $s.strings = $a.$side.strings }
			$entry[$side] = $s
		}
		$entry
	}
	$json = ([ordered]@{ areas = @($out) } | ConvertTo-Json -Depth 6) -replace "`r`n", "`n"
	[System.IO.File]::WriteAllText($ManifestPath, $json + "`n")
}

function Set-AreaBaseline {
	param([Parameter(Mandatory)][string]$ManifestPath, [Parameter(Mandatory)][string]$Id, [Parameter(Mandatory)][string]$Sha)
	if ($Sha -notmatch '^[0-9a-f]{40}$') { throw "baseline must be a full 40-character commit SHA, got '$Sha'" }
	$areas = @(Get-SyncAreas -ManifestPath $ManifestPath)
	$target = @($areas | Where-Object id -EQ $Id)
	if ($target.Count -eq 0) { throw "area '$Id' not found in $ManifestPath" }
	$target[0].baseline = $Sha
	Write-SyncAreas -ManifestPath $ManifestPath -Areas $areas
}

#endregion

Export-ModuleMember -Function *-*
