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

#region seeding

function Get-MuxReferences {
	param([Parameter(Mandatory)][string[]]$Paths)
	$pattern = [regex]::new('MUX Reference\b.*?\b(commit|tag)\s*:?\s*(winui3/release/[\w.\-]+|[0-9a-f]{7,40}\b)', 'IgnoreCase')
	foreach ($file in Get-ChildItem -LiteralPath $Paths -Recurse -File -Filter *.cs | Sort-Object FullName) {
		$seen = [ordered]@{}
		foreach ($m in $pattern.Matches((Get-Content -Raw -LiteralPath $file.FullName))) {
			$kind = $m.Groups[1].Value.ToLowerInvariant()
			$ref = $m.Groups[2].Value.TrimEnd('.')
			if ($kind -eq 'commit') { $ref = $ref.ToLowerInvariant() }
			$seen["$kind $ref"] = [pscustomobject]@{ File = $file.FullName; Kind = $kind; Ref = $ref }
		}
		if ($seen.Count -eq 0) {
			[pscustomobject]@{ File = $file.FullName; Kind = 'none'; Ref = $null }
		}
		else {
			$seen.Values
		}
	}
}

function Resolve-AreaBaseline {
	param([Parameter(Mandatory)][string]$UpstreamRepo, [Parameter(Mandatory)][object[]]$References)
	$resolved = [ordered]@{}
	$unresolved = [System.Collections.Generic.List[string]]::new()
	foreach ($r in $References | Where-Object Kind -NE 'none' | Sort-Object Ref -Unique) {
		$spec = if ($r.Kind -eq 'tag') { "refs/tags/$($r.Ref)^{commit}" } else { "$($r.Ref)^{commit}" }
		$sha = @(Invoke-Git $UpstreamRepo @('rev-parse', '--verify', '--quiet', $spec) -AllowExitCodes 1, 128)
		if ((Get-LastGitExitCode) -eq 0 -and $sha.Count -eq 1) { $resolved[$r.Ref] = $sha[0] } else { $unresolved.Add($r.Ref) }
	}

	$candidates = @($resolved.Values | Sort-Object -Unique)
	$baseline = $null
	foreach ($c in $candidates) {
		$isAncestorOfAll = $true
		foreach ($o in $candidates) {
			Invoke-Git $UpstreamRepo @('merge-base', '--is-ancestor', $c, $o) -AllowExitCodes 1 | Out-Null
			if ((Get-LastGitExitCode) -ne 0) { $isAncestorOfAll = $false; break }
		}
		if ($isAncestorOfAll) { $baseline = $c; break }
	}
	if (-not $baseline -and $candidates.Count -gt 0) {
		$baseline = $candidates | Sort-Object { [long](Invoke-Git $UpstreamRepo @('log', '-1', '--format=%ct', $_)) } | Select-Object -First 1
	}

	[pscustomobject]@{
		Baseline   = $baseline
		Resolved   = $resolved
		Unresolved = @($unresolved)
		NoRef      = @($References | Where-Object Kind -EQ 'none' | ForEach-Object File)
	}
}

#endregion

#region detection

function New-TempDirectory {
	$dir = Join-Path ([System.IO.Path]::GetTempPath()) "winui-sync-$([guid]::NewGuid().ToString('N'))"
	New-Item -ItemType Directory -Path $dir | Out-Null
	$dir
}

# Extracts <Path> of <Repo> at <Ref> and returns the extracted folder. Throws if the path does not exist there.
function Export-GitTree {
	param([Parameter(Mandatory)][string]$Repo, [Parameter(Mandatory)][string]$Ref, [Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Destination)
	$Path = $Path.TrimEnd('/')
	$found = @(Invoke-Git $Repo @('ls-tree', '-d', $Ref, '--', $Path))
	if ($found.Count -eq 0) { throw "path '$Path' not found at $Ref" }
	$zip = Join-Path $Destination 'tree.zip'
	Invoke-Git $Repo @('-c', 'core.autocrlf=false', 'archive', '--format=zip', '-o', $zip, $Ref, '--', $Path) | Out-Null
	Expand-Archive -LiteralPath $zip -DestinationPath $Destination
	Remove-Item -LiteralPath $zip
	Join-Path $Destination $Path
}

function Get-TreeFingerprint {
	param([string]$Dir)
	$map = @{}
	if (-not $Dir -or -not (Test-Path -LiteralPath $Dir)) { return $map }
	$root = (Resolve-Path -LiteralPath $Dir).Path
	$sha = [System.Security.Cryptography.SHA256]::Create()
	foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
		$rel = [System.IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
		# Line endings are whatever each repo's attributes make them; they are not a content change.
		$bytes = [System.IO.File]::ReadAllBytes($file.FullName) | Where-Object { $_ -ne 13 }
		$map[$rel.ToLowerInvariant()] = [pscustomobject]@{ Path = $rel; Hash = [System.Convert]::ToHexString($sha.ComputeHash([byte[]]@($bytes))) }
	}
	$map
}

# Differences that replacing <UnoDir> with <UpstreamDir> would make. Paths compare case-insensitively.
function Compare-StringsTree {
	param([Parameter(Mandatory)][string]$UpstreamDir, [string]$UnoDir)
	$up = Get-TreeFingerprint $UpstreamDir
	$uno = Get-TreeFingerprint $UnoDir
	[pscustomobject]@{
		Added   = @($up.Keys | Where-Object { -not $uno.ContainsKey($_) } | ForEach-Object { $up[$_].Path } | Sort-Object)
		Removed = @($uno.Keys | Where-Object { -not $up.ContainsKey($_) } | ForEach-Object { $uno[$_].Path } | Sort-Object)
		Changed = @($up.Keys | Where-Object { $uno.ContainsKey($_) -and $uno[$_].Hash -ne $up[$_].Hash } | ForEach-Object { $up[$_].Path } | Sort-Object)
	}
}

function Test-StringsDiffer {
	param(
		[Parameter(Mandatory)][string]$UpstreamRepo,
		[Parameter(Mandatory)][string]$Ref,
		[Parameter(Mandatory)][string]$UpstreamStrings,
		[Parameter(Mandatory)][string]$UnoStrings)
	$tmp = New-TempDirectory
	try {
		$resolved = Resolve-UpstreamPath -UpstreamRepo $UpstreamRepo -Ref $Ref -Path $UpstreamStrings
		if (-not $resolved) { throw "upstream strings path '$UpstreamStrings' not found at $Ref" }
		$upstreamDir = Export-GitTree -Repo $UpstreamRepo -Ref $Ref -Path $resolved -Destination $tmp
		$diff = Compare-StringsTree -UpstreamDir $upstreamDir -UnoDir $UnoStrings
		($diff.Added.Count + $diff.Removed.Count + $diff.Changed.Count) -gt 0
	}
	finally {
		Remove-Item -Recurse -Force -LiteralPath $tmp -ErrorAction SilentlyContinue
	}
}

# Upstream moved everything under src/ at some point, and release tags are not descendants of
# older baselines. Manifest paths are layout-neutral; this finds where one lives at a given commit.
function Resolve-UpstreamPath {
	param([Parameter(Mandatory)][string]$UpstreamRepo, [Parameter(Mandatory)][string]$Ref, [Parameter(Mandatory)][string]$Path)
	$Path = $Path.Trim('/')
	foreach ($candidate in $Path, "src/$Path") {
		if (@(Invoke-Git $UpstreamRepo @('ls-tree', '-d', $Ref, '--', $candidate)).Count -gt 0) { return $candidate }
	}
	$null
}

# One tree-to-tree comparison per top-level area path: @{ Path; Base; Target; Exclude }.
# Base/Target are '<sha>:<path>' tree-ish specs (the empty tree when the path is absent on that side).
function Get-AreaTreeDiffs {
	param([Parameter(Mandatory)]$Area, [Parameter(Mandatory)][string]$UpstreamRepo, [Parameter(Mandatory)][string]$TargetSha)
	$emptyTree = '4b825dc642cb6eb9a060e54bf8d69288fbee4904'
	$all = @(@($Area.upstream.sources) + @($Area.upstream.tests) | ForEach-Object { $_.Trim('/') } | Sort-Object -Unique)
	$roots = @($all | Where-Object { $p = $_; -not ($all | Where-Object { $p.StartsWith("$_/") }) })
	foreach ($path in $roots) {
		$atBase = Resolve-UpstreamPath -UpstreamRepo $UpstreamRepo -Ref $Area.baseline -Path $path
		$atTarget = Resolve-UpstreamPath -UpstreamRepo $UpstreamRepo -Ref $TargetSha -Path $path
		$exclude = @()
		$strings = if ($Area.upstream.strings) { $Area.upstream.strings.Trim('/') } else { $null }
		if ($strings -and $strings.StartsWith("$path/")) { $exclude = @(":(exclude)$($strings.Substring($path.Length + 1))") }
		[pscustomobject]@{
			Path    = $path
			Base    = if ($atBase) { "$($Area.baseline):$atBase" } else { $emptyTree }
			Target  = if ($atTarget) { "${TargetSha}:$atTarget" } else { $emptyTree }
			Exclude = $exclude
		}
	}
}

function Get-AreaSyncState {
	param(
		[Parameter(Mandatory)]$Area,
		[Parameter(Mandatory)][string]$UpstreamRepo,
		[Parameter(Mandatory)][string]$TargetSha,
		[Parameter(Mandatory)][string]$UnoRoot,
		# Compare strings against this folder instead of <UnoRoot>/<uno.strings> (used when a sync branch is open).
		[string]$UnoStringsDir)
	Invoke-Git $UpstreamRepo @('cat-file', '-e', "$($Area.baseline)^{commit}") -AllowExitCodes 1, 128 | Out-Null
	if ((Get-LastGitExitCode) -ne 0) { throw "area '$($Area.id)': baseline $($Area.baseline) is not a commit in $UpstreamRepo" }

	$codeChanged = $false
	$stats = @()
	foreach ($d in Get-AreaTreeDiffs -Area $Area -UpstreamRepo $UpstreamRepo -TargetSha $TargetSha) {
		$tail = if ($d.Exclude) { @('--', '.') + $d.Exclude } else { @() }
		Invoke-Git $UpstreamRepo (@('diff', '--quiet', $d.Base, $d.Target) + $tail) -AllowExitCodes 1 | Out-Null
		if ((Get-LastGitExitCode) -eq 1) {
			$codeChanged = $true
			$stats += "$(Invoke-Git $UpstreamRepo (@('diff', '--shortstat', $d.Base, $d.Target) + $tail))".Trim()
		}
	}
	$diffStat = $stats -join '; '

	$stringsChanged = $false
	if ($Area.upstream.strings -and $Area.uno.strings) {
		$unoDir = if ($UnoStringsDir) { $UnoStringsDir } else { Join-Path $UnoRoot $Area.uno.strings }
		$stringsChanged = Test-StringsDiffer -UpstreamRepo $UpstreamRepo -Ref $TargetSha -UpstreamStrings $Area.upstream.strings -UnoStrings $unoDir
	}

	[pscustomobject]@{ id = $Area.id; codeChanged = $codeChanged; stringsChanged = $stringsChanged; diffStat = $diffStat }
}

function Select-SyncMatrix {
	param([Parameter(Mandatory)][object[]]$States, [Parameter(Mandatory)][object[]]$Areas, [Parameter(Mandatory)][string]$UpstreamRepo, [int]$MaxAreas = 10)
	$changed = @($States | Where-Object { $_.codeChanged -or $_.stringsChanged } | ForEach-Object {
			$id = $_.id
			$area = $Areas | Where-Object id -EQ $id | Select-Object -First 1
			[pscustomobject]@{ id = $id; date = [long](Invoke-Git $UpstreamRepo @('log', '-1', '--format=%ct', $area.baseline)) }
		} | Sort-Object date, id)
	[pscustomobject]@{
		Selected = @($changed | Select-Object -First $MaxAreas | ForEach-Object id)
		Deferred = @($changed | Select-Object -Skip $MaxAreas | ForEach-Object id)
	}
}

function Get-SyncBranchName {
	param([Parameter(Mandatory)][string]$Id)
	"sync/winui/$Id"
}

function Get-OpenSyncPr {
	param([Parameter(Mandatory)][string]$Id)
	$number = & gh pr list --head (Get-SyncBranchName $Id) --state open --json number --jq '.[0].number // empty'
	if ($LASTEXITCODE -ne 0) { throw "gh pr list failed for area '$Id'" }
	if ("$number".Trim()) { [int]"$number".Trim() } else { $null }
}

function Get-SyncPlan {
	param(
		[Parameter(Mandatory)][string]$UpstreamRepo,
		[Parameter(Mandatory)][string]$UnoRoot,
		[Parameter(Mandatory)][string]$ManifestPath,
		[string]$AreaFilter,
		[string]$TargetTag,
		[int]$MaxAreas = 10,
		[string]$DefaultModel = 'claude-opus-5-5')
	$target = if ($TargetTag) {
		[pscustomobject]@{ Tag = $TargetTag; Sha = (Invoke-Git $UpstreamRepo @('rev-list', '-n1', "refs/tags/$TargetTag")) }
	}
	else { Get-LatestStableTag -UpstreamRepo $UpstreamRepo }
	$masterSha = Invoke-Git $UnoRoot @('rev-parse', 'HEAD')
	$manifestRel = [System.IO.Path]::GetRelativePath((Resolve-Path -LiteralPath $UnoRoot).Path, (Resolve-Path -LiteralPath $ManifestPath).Path).Replace('\', '/')

	$areas = @(Get-SyncAreas -ManifestPath $ManifestPath | Where-Object { -not $AreaFilter -or $_.id -eq $AreaFilter })
	$states = @()
	$prs = @{}
	foreach ($area in $areas) {
		$tmp = $null
		try {
			$stringsDir = $null
			$pr = Get-OpenSyncPr -Id $area.id
			if ($pr) {
				# An open sync PR already carries part of the work: continue from its baseline and strings.
				$prs[$area.id] = $pr
				$branchRef = "origin/$(Get-SyncBranchName $area.id)"
				$branchJson = (Invoke-Git $UnoRoot @('show', "${branchRef}:$manifestRel")) -join "`n"
				$onBranch = @(Get-SyncAreasFromJson -Json $branchJson | Where-Object id -EQ $area.id)
				if ($onBranch.Count -eq 1) { $area.baseline = $onBranch[0].baseline }
				if ($area.uno.strings) {
					$tmp = New-TempDirectory
					$stringsDir = try { Export-GitTree -Repo $UnoRoot -Ref $branchRef -Path $area.uno.strings -Destination $tmp } catch { Join-Path $tmp 'missing' }
				}
			}
			$states += Get-AreaSyncState -Area $area -UpstreamRepo $UpstreamRepo -TargetSha $target.Sha -UnoRoot $UnoRoot -UnoStringsDir $stringsDir
		}
		finally {
			if ($tmp) { Remove-Item -Recurse -Force -LiteralPath $tmp -ErrorAction SilentlyContinue }
		}
	}

	$selection = if ($states.Count -gt 0) { Select-SyncMatrix -States $states -Areas $areas -UpstreamRepo $UpstreamRepo -MaxAreas $MaxAreas } else { [pscustomobject]@{ Selected = @(); Deferred = @() } }
	$matrix = @(foreach ($id in $selection.Selected) {
			$area = $areas | Where-Object id -EQ $id
			$state = $states | Where-Object id -EQ $id
			[pscustomobject][ordered]@{
				id             = $id
				title          = $area.title
				model          = if ($area.model) { $area.model } else { $DefaultModel }
				baseline       = $area.baseline
				codeChanged    = [bool]$state.codeChanged
				stringsChanged = [bool]$state.stringsChanged
				branch         = Get-SyncBranchName $id
				prNumber       = if ($prs.ContainsKey($id)) { [int]$prs[$id] } else { 0 }
			}
		})
	$rows = @(foreach ($state in $states) {
			$area = $areas | Where-Object id -EQ $state.id
			[pscustomobject]@{
				id       = $state.id
				baseline = $area.baseline
				status   = if ($selection.Deferred -contains $state.id) { 'deferred' }
				elseif ($state.codeChanged -and $state.stringsChanged) { 'code + strings changed' }
				elseif ($state.codeChanged) { 'code changed' }
				elseif ($state.stringsChanged) { 'strings changed' }
				else { 'up to date' }
				diffStat = $state.diffStat
				prNumber = if ($prs.ContainsKey($state.id)) { [int]$prs[$state.id] } else { 0 }
			}
		})

	[pscustomobject]@{ TargetTag = $target.Tag; TargetSha = $target.Sha; MasterSha = $masterSha; Matrix = $matrix; Rows = $rows }
}

#endregion

Export-ModuleMember -Function *-*
