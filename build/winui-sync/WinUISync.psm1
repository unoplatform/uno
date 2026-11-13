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
	$all = @(@($Area.upstream.sources) + @($Area.upstream.tests) | Where-Object { $_ } | ForEach-Object { $_.Trim('/') } | Sort-Object -Unique)
	$resolved = @{}
	foreach ($path in $all) {
		$resolved[$path] = @{
			Base   = Resolve-UpstreamPath -UpstreamRepo $UpstreamRepo -Ref $Area.baseline -Path $path
			Target = Resolve-UpstreamPath -UpstreamRepo $UpstreamRepo -Ref $TargetSha -Path $path
		}
		# A mistyped path would otherwise read as "nothing ever changes here".
		if (-not $resolved[$path].Base -and -not $resolved[$path].Target) {
			throw "area '$($Area.id)': upstream path '$path' exists at neither $($Area.baseline) nor $TargetSha"
		}
	}
	$roots = @($all | Where-Object { $p = $_; -not ($all | Where-Object { $p.StartsWith("$_/") }) })
	foreach ($path in $roots) {
		$atBase = $resolved[$path].Base
		$atTarget = $resolved[$path].Target
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

# Anyone can open a PR from a fork whose head branch has the same name; those are not ours.
function Select-OpenSyncPr {
	param([Parameter(Mandatory)][string]$Json)
	$own = @($Json | ConvertFrom-Json | Where-Object { $_ -and -not $_.isCrossRepository } | Sort-Object number)
	if ($own.Count -gt 0) { [int]$own[0].number } else { $null }
}

function Get-OpenSyncPr {
	param([Parameter(Mandatory)][string]$Id)
	$json = & gh pr list --head (Get-SyncBranchName $Id) --state open --json 'number,isCrossRepository'
	if ($LASTEXITCODE -ne 0) { throw "gh pr list failed for area '$Id'" }
	Select-OpenSyncPr -Json ($json -join "`n")
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
	$errors = [ordered]@{}
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
		catch {
			$errors[$area.id] = ($_.Exception.Message -split "`n")[0]
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
	$rows += @(foreach ($id in $errors.Keys) {
			$area = $areas | Where-Object id -EQ $id
			[pscustomobject]@{ id = $id; baseline = $area.baseline; status = "error: $($errors[$id])"; diffStat = ''; prNumber = if ($prs.ContainsKey($id)) { [int]$prs[$id] } else { 0 } }
		})

	[pscustomobject]@{ TargetTag = $target.Tag; TargetSha = $target.Sha; MasterSha = $masterSha; Matrix = $matrix; Rows = $rows; Errors = @($errors.Keys) }
}

#endregion

#region strings

# Makes <UnoStrings> match upstream's Strings folder at <Ref>: content replaced, new files added,
# files upstream no longer has removed. Nothing is touched unless the upstream folder was read successfully.
function Sync-AreaStrings {
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
		$up = Get-TreeFingerprint $upstreamDir
		if ($up.Count -eq 0) { throw "upstream strings path '$UpstreamStrings' is empty at $Ref" }
		$uno = Get-TreeFingerprint $UnoStrings

		New-Item -ItemType Directory -Force -Path $UnoStrings | Out-Null
		$unoRoot = (Resolve-Path -LiteralPath $UnoStrings).Path
		# Existing folders keep their casing (en-US vs upstream's en-us), so a sync never renames every locale.
		$dirCasing = @{}
		foreach ($dir in Get-ChildItem -LiteralPath $unoRoot -Recurse -Directory) {
			$rel = [System.IO.Path]::GetRelativePath($unoRoot, $dir.FullName).Replace('\', '/')
			$dirCasing[$rel.ToLowerInvariant()] = $rel
		}

		$added = @(); $changed = @(); $removed = @()
		foreach ($key in $up.Keys | Sort-Object) {
			$source = Join-Path $upstreamDir $up[$key].Path
			if ($uno.ContainsKey($key)) {
				if ($uno[$key].Hash -eq $up[$key].Hash) { continue }
				Copy-Item -LiteralPath $source -Destination (Join-Path $unoRoot $uno[$key].Path) -Force
				$changed += $uno[$key].Path
			}
			else {
				$relDir = [System.IO.Path]::GetDirectoryName($up[$key].Path).Replace('\', '/')
				if ($relDir -and $dirCasing.ContainsKey($relDir.ToLowerInvariant())) { $relDir = $dirCasing[$relDir.ToLowerInvariant()] }
				$rel = if ($relDir) { "$relDir/$([System.IO.Path]::GetFileName($up[$key].Path))" } else { $up[$key].Path }
				$dest = Join-Path $unoRoot $rel
				New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
				Copy-Item -LiteralPath $source -Destination $dest -Force
				$added += $rel
			}
		}
		foreach ($key in $uno.Keys | Where-Object { -not $up.ContainsKey($_) } | Sort-Object) {
			Remove-Item -LiteralPath (Join-Path $unoRoot $uno[$key].Path) -Force
			$removed += $uno[$key].Path
		}
		Get-ChildItem -LiteralPath $unoRoot -Recurse -Directory | Sort-Object { $_.FullName.Length } -Descending |
			Where-Object { -not (Get-ChildItem -LiteralPath $_.FullName -Force) } | Remove-Item -Force

		[pscustomobject]@{ Added = @($added); Removed = @($removed); Changed = @($changed) }
	}
	finally {
		Remove-Item -Recurse -Force -LiteralPath $tmp -ErrorAction SilentlyContinue
	}
}

#endregion

#region report and bodies

$script:BodyLimit = 60000

function Get-TagVersion {
	param([Parameter(Mandatory)][string]$TargetTag)
	if ($TargetTag -match $script:StableTagPattern) { $Matches[1] } else { $TargetTag }
}

# Upstream text must not ping people or link to this repository's issues when posted here.
function ConvertTo-SafeReport {
	param([Parameter(Position = 0)][AllowEmptyString()][string]$Text)
	$parts = [regex]::Split($Text, '(```[\s\S]*?```|`[^`\n]*`)')
	$out = for ($i = 0; $i -lt $parts.Count; $i++) {
		$part = $parts[$i]
		if ($i % 2 -eq 1) { $part; continue }
		$part = [regex]::Replace($part, '(?<![\w`/.@])@([A-Za-z0-9][A-Za-z0-9-]*(?:/[A-Za-z0-9._-]+)?)', '`@$1`')
		$part = [regex]::Replace($part, '(?<![\w/#&])#(\d+)\b', "$script:UpstreamSlug#`$1")
		[regex]::Replace($part, '(?<![\w-])GH-(\d+)\b', "$script:UpstreamSlug#`$1")
	}
	-join $out
}

# Anything other than a well-formed report counts as "build failed, something skipped",
# so a broken agent run can never produce a PR that closes its issue.
function Read-SyncReport {
	param([Parameter(Mandatory)][string]$Path)
	if (-not (Test-Path -LiteralPath $Path)) {
		return [pscustomobject]@{ Valid = $false; BuildPassed = $false; SkippedCount = 1; Body = '_The port step did not produce a report. Treat this sync as unverified._' }
	}
	$text = (Get-Content -Raw -LiteralPath $Path) -replace "`r`n", "`n"
	$m = [regex]::Match($text, '\A\s*build:\s*(passed|failed)\s*\n\s*skipped:\s*(\d+)\s*\n\s*---[ \t]*\n?')
	if (-not $m.Success) {
		return [pscustomobject]@{
			Valid        = $false
			BuildPassed  = $false
			SkippedCount = 1
			Body         = "_The report did not follow the expected format. Treat this sync as unverified._`n`n" + (ConvertTo-SafeReport $text)
		}
	}
	[pscustomobject]@{
		Valid        = $true
		BuildPassed  = $m.Groups[1].Value -eq 'passed'
		SkippedCount = [int]$m.Groups[2].Value
		Body         = (ConvertTo-SafeReport $text.Substring($m.Length)).Trim()
	}
}

# Reads the per-branch report file: every run on the branch is one section, newest first.
function Get-SyncRunSummary {
	param([AllowEmptyString()][string]$Text)
	$runs = [regex]::Matches("$Text", '<!-- winui-sync-run target=(\S+) build=(passed|failed) skipped=(\d+) -->')
	$skipped = 0
	foreach ($run in $runs) { $skipped += [int]$run.Groups[3].Value }
	[pscustomobject]@{
		Valid        = $true
		# The newest build covers everything on the branch; skipped items add up until someone ports them.
		BuildPassed  = $runs.Count -gt 0 -and $runs[0].Groups[2].Value -eq 'passed'
		SkippedCount = $skipped
		Body         = "$Text".Trim()
	}
}

# Adds this run to the area's report file and returns the summary of all runs on the branch.
function Add-SyncRunReport {
	param(
		[Parameter(Mandatory)][string]$Path,
		[Parameter(Mandatory)]$Area,
		[Parameter(Mandatory)][string]$TargetTag,
		[Parameter(Mandatory)][string]$TargetSha,
		[Parameter(Mandatory)]$Report,
		# A new branch starts a new report; a file inherited from the default branch belongs to a merged sync.
		[bool]$Reset)
	$earlier = if (-not $Reset -and (Test-Path -LiteralPath $Path)) { ((Get-Content -Raw -LiteralPath $Path) -replace "`r`n", "`n").Trim() } else { '' }
	$section = @(
		"<!-- winui-sync-run target=$TargetTag build=$(if ($Report.BuildPassed) { 'passed' } else { 'failed' }) skipped=$($Report.SkippedCount) -->"
		"**Sync to WinUI $(Get-TagVersion $TargetTag)** (``$($Area.baseline.Substring(0, 10))`` → ``$($TargetSha.Substring(0, 10))``)"
		''
		$Report.Body.Trim()
	) -join "`n"
	$text = if ($earlier) { "$section`n`n---`n`n$earlier" } else { $section }
	New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
	[System.IO.File]::WriteAllText($Path, $text + "`n")
	Get-SyncRunSummary -Text $text
}

function Limit-BodyText {
	param([string]$Text, [int]$Max)
	if ($Text.Length -le $Max) { return $Text }
	$Text.Substring(0, $Max) + "`n`n_…truncated; see the workflow run for the full report._"
}

function Format-StringsChanges {
	param($StringsChanges)
	if (-not $StringsChanges) { return '' }
	$total = @($StringsChanges.Added).Count + @($StringsChanges.Removed).Count + @($StringsChanges.Changed).Count
	if ($total -eq 0) { return '' }
	$lines = @('### Translations', '', "Replaced from upstream: $(@($StringsChanges.Changed).Count) changed, $(@($StringsChanges.Added).Count) added, $(@($StringsChanges.Removed).Count) removed.", '')
	foreach ($kind in 'Added', 'Removed', 'Changed') {
		$files = @($StringsChanges.$kind)
		if ($files.Count -eq 0) { continue }
		$shown = @($files | Select-Object -First 20 | ForEach-Object { "``$_``" }) -join ', '
		$more = if ($files.Count -gt 20) { " and $($files.Count - 20) more" } else { '' }
		$lines += "- **${kind}:** $shown$more"
	}
	$lines -join "`n"
}

function Get-SyncMarker {
	param([Parameter(Mandatory)][string]$Id)
	"<!-- winui-sync:$Id -->"
}

function New-SyncIssue {
	param([Parameter(Mandatory)]$Area, [Parameter(Mandatory)][string]$TargetTag, [Parameter(Mandatory)][string]$TargetSha, [Parameter(Mandatory)]$Report, $StringsChanges)
	$version = Get-TagVersion $TargetTag
	$strings = Format-StringsChanges $StringsChanges
	$head = @(
		(Get-SyncMarker $Area.id)
		'### What would you like to be added? 💡'
		''
		"Sync the Uno port of **$($Area.title)** with WinUI ``$version``."
		''
		"Upstream range: ``$($Area.baseline.Substring(0, 10))`` → ``$($TargetSha.Substring(0, 10))`` (``$TargetTag``) · [compare](https://github.com/$script:UpstreamSlug/compare/$($Area.baseline)...$TargetSha)"
		"Port build: **$(if ($Report.BuildPassed) { 'passed' } else { 'failed or not verified' })** · Skipped items: **$($Report.SkippedCount)**"
		''
		''
	) -join "`n"
	$tail = @(
		''
		$strings
		''
		'### Why is this needed? 🤔'
		''
		"Keeps $($Area.title) aligned with the latest stable WinUI release."
		''
		'### For which platforms 📱💻🖥️'
		''
		'All'
		''
		'### Anything else we need to know? 💬'
		''
		'Generated by the WinUI port sync workflow. This issue stays open while anything is listed as skipped or the port build is failing.'
	) -join "`n"
	[pscustomobject]@{
		Title = "[WinUI Sync] $($Area.title) → WinUI $version"
		Body  = $head + (Limit-BodyText $Report.Body ($script:BodyLimit - $head.Length - $tail.Length)) + "`n" + $tail
	}
}

function New-SyncPr {
	param([Parameter(Mandatory)]$Area, [Parameter(Mandatory)][string]$TargetTag, [Parameter(Mandatory)]$Report, [Parameter(Mandatory)][int]$IssueNumber)
	$version = Get-TagVersion $TargetTag
	$complete = $Report.BuildPassed -and $Report.SkippedCount -eq 0
	$title = "fix($($Area.id)): Sync $($Area.title) with WinUI $version"
	if (-not $Report.BuildPassed) { $title = "[build failing] $title" }
	$head = @(
		"**GitHub Issue:** $(if ($complete) { 'closes' } else { 'related to' }) #$IssueNumber"
		''
		(Get-SyncMarker $Area.id)
		''
		'## PR Type:'
		''
		'💬 Other: automated sync of ported WinUI code'
		''
		'## What changed? 🚀'
		''
		"Ports the upstream changes to **$($Area.title)** up to WinUI ``$version`` and moves its sync baseline."
		''
		"- Port build: **$(if ($Report.BuildPassed) { 'passed' } else { 'failed or not verified' })** (no tests were run)"
		"- Skipped items: **$($Report.SkippedCount)**$(if (-not $complete) { " — #$IssueNumber stays open to track what is left" })"
		''
		''
	) -join "`n"
	$tail = @(
		''
		'## PR Checklist ✅'
		''
		'- [ ] 🧪 Added [Runtime tests, UI tests, or a manual test sample](https://github.com/unoplatform/uno/blob/master/doc/articles/uno-development/working-with-the-samples-apps.md) (for bug fixes / features, if applicable)'
		'- [ ] 📚 Docs have been added/updated following the [documentation template](https://github.com/unoplatform/uno/blob/master/doc/.feature-template.md) (for bug fixes / features)'
		'- [ ] 🖼️ Validated PR `Screenshots Compare Test Run` results.'
		'- [ ] ❗ Contains **NO** breaking changes'
		'- [ ] 👀 Reviewed 2 other [open pull requests](https://github.com/unoplatform/uno/pulls) (optional but appreciated!)'
	) -join "`n"
	[pscustomobject]@{
		Title = $title
		Body  = $head + (Limit-BodyText $Report.Body ($script:BodyLimit - $head.Length - $tail.Length)) + "`n" + $tail
	}
}

function Get-CommitGroups {
	param([Parameter(Mandatory)]$Area, [Parameter(Mandatory)][string]$TargetTag, [string[]]$ChangedFiles = @(), [Parameter(Mandatory)][string]$ManifestRelativePath)
	$version = Get-TagVersion $TargetTag
	$under = { param($file, $roots) [bool](@($roots) | Where-Object { $_ -and ($file -eq $_.Trim('/') -or $file.StartsWith("$($_.Trim('/'))/")) }) }
	$buckets = [ordered]@{ logic = @(); tests = @(); strings = @(); manifest = @() }
	foreach ($file in @($ChangedFiles) | Where-Object { $_ } | Sort-Object) {
		$reports = "$(Split-Path $ManifestRelativePath)/reports/".Replace('\', '/')
		$kind = if ($file -eq $ManifestRelativePath -or $file.StartsWith($reports)) { 'manifest' }
		elseif (& $under $file @($Area.uno.strings)) { 'strings' }
		elseif (& $under $file @($Area.uno.tests)) { 'tests' }
		else { 'logic' }
		$buckets[$kind] += $file
	}
	$messages = @{
		logic    = "fix($($Area.id)): Sync $($Area.title) with WinUI $version"
		tests    = "test($($Area.id)): Sync $($Area.title) tests with WinUI $version"
		strings  = "chore($($Area.id)): Update $($Area.title) strings from WinUI $version"
		manifest = "chore($($Area.id)): Bump WinUI sync baseline to $version"
	}
	foreach ($kind in $buckets.Keys) {
		if ($buckets[$kind].Count -gt 0) { [pscustomobject]@{ Message = $messages[$kind]; Files = @($buckets[$kind]) } }
	}
}

#endregion

#region prompt

function Expand-SyncPrompt {
	param(
		[Parameter(Mandatory)][string]$TemplatePath,
		[Parameter(Mandatory)]$Area,
		[Parameter(Mandatory)][string]$TargetTag,
		[Parameter(Mandatory)][string]$TargetSha,
		# Clone used to resolve paths; -UpstreamDir is how the agent should refer to it.
		[Parameter(Mandatory)][string]$UpstreamRepo,
		[Parameter(Mandatory)][string]$UpstreamDir)
	$commands = foreach ($d in Get-AreaTreeDiffs -Area $Area -UpstreamRepo $UpstreamRepo -TargetSha $TargetSha) {
		$tail = if ($d.Exclude) { " -- . " + (($d.Exclude | ForEach-Object { "'$_'" }) -join ' ') } else { '' }
		"      git -C $UpstreamDir diff $($d.Base) $($d.Target)$tail"
	}
	$list = { param($items) if (@($items).Count -gt 0) { (@($items) | ForEach-Object { "``$_``" }) -join ', ' } else { '_none_' } }
	$values = @{
		id            = $Area.id
		title         = $Area.title
		baseline      = $Area.baseline
		target_tag    = $TargetTag
		target_sha    = $TargetSha
		upstream_dir  = $UpstreamDir
		diff_commands = $commands -join "`n"
		uno_sources   = & $list $Area.uno.sources
		uno_tests     = & $list $Area.uno.tests
	}
	$text = (Get-Content -Raw -LiteralPath $TemplatePath) -replace "`r`n", "`n"
	$text = [regex]::Replace($text, '\{\{(\w+)\}\}', {
			param($m)
			$name = $m.Groups[1].Value
			if (-not $values.ContainsKey($name)) { throw "unknown placeholder {{$name}} in $TemplatePath" }
			[string]$values[$name]
		})
	$text
}

#endregion

#region branch and publish

# 'ok' only when the port job's result can be trusted for this area.
function Get-PortStatus {
	param([string]$ArtifactDir, [bool]$CodeChanged)
	$statusFile = if ($ArtifactDir) { Join-Path $ArtifactDir 'status.json' } else { $null }
	if (-not $statusFile -or -not (Test-Path -LiteralPath $statusFile)) { return 'missing' }
	$status = Get-Content -Raw -LiteralPath $statusFile | ConvertFrom-Json
	if ("$(Get-OptionalProperty $status 'conflict')" -eq 'true') { return 'conflict' }
	$agent = "$(Get-OptionalProperty $status 'agentOutcome')"
	# A failed, timed-out or cancelled agent leaves a partial port; publishing it would move the baseline past work not done.
	if ($CodeChanged -and $agent -ne 'success') { return "agent $agent".Trim() }
	'ok'
}

# Returns the files a port patch may not contain. The port job filters too, but that job runs the agent;
# this check runs where the write token is.
function Test-SyncPatchPaths {
	param([string[]]$Files = @())
	@($Files | Where-Object { $_ } | Where-Object { -not $_.StartsWith('src/') -or $_ -match '(^|/)Strings/.+\.resw$' })
}

function Invoke-ApplyPatch {
	param([Parameter(Mandatory)][string]$RepoRoot, [Parameter(Mandatory)][string]$PatchPath)
	if (-not (Test-Path -LiteralPath $PatchPath) -or (Get-Item -LiteralPath $PatchPath).Length -eq 0) { return $true }
	$head = Invoke-Git $RepoRoot @('rev-parse', 'HEAD')
	Invoke-Git $RepoRoot @('apply', '--index', '--binary', (Resolve-Path -LiteralPath $PatchPath).Path) -AllowExitCodes 1, 128 | Out-Null
	if ((Get-LastGitExitCode) -eq 0) { return $true }
	Invoke-Git $RepoRoot @('reset', '-q', '--hard', $head) | Out-Null
	$false
}

# Puts the working tree on the area's sync branch. Returns 'ready' or 'conflict'. Never pushes.
function Initialize-SyncBranch {
	param([Parameter(Mandatory)][string]$RepoRoot, [Parameter(Mandatory)][string]$Id, [Parameter(Mandatory)][string]$MasterSha, [switch]$HasOpenPr)
	$branch = Get-SyncBranchName $Id
	if (-not $HasOpenPr) {
		# No open PR: whatever is on the remote branch belongs to a closed PR and is not continued.
		Invoke-Git $RepoRoot @('checkout', '-q', '-B', $branch, $MasterSha) | Out-Null
		return 'ready'
	}
	Invoke-Git $RepoRoot @('checkout', '-q', '-B', $branch, "origin/$branch") | Out-Null
	$identity = @('-c', 'user.name=github-actions[bot]', '-c', 'user.email=41898282+github-actions[bot]@users.noreply.github.com')
	Invoke-Git $RepoRoot ($identity + @('merge', '-q', '--no-edit', $MasterSha)) -AllowExitCodes 1, 128 | Out-Null
	if ((Get-LastGitExitCode) -eq 0) { return 'ready' }
	Invoke-Git $RepoRoot @('merge', '--abort') -AllowExitCodes 1, 128 | Out-Null
	Invoke-Git $RepoRoot @('reset', '-q', '--hard', "origin/$branch") | Out-Null
	'conflict'
}

function Invoke-Gh {
	param([Parameter(Mandatory)][string[]]$GhArgs)
	$out = & gh @GhArgs
	if ($LASTEXITCODE -ne 0) { throw "gh $($GhArgs[0..1] -join ' ') failed ($LASTEXITCODE)" }
	$out
}

function Find-SyncIssue {
	param([Parameter(Mandatory)][string]$Id)
	$marker = Get-SyncMarker $Id
	$issues = Invoke-Gh @('issue', 'list', '--state', 'open', '--label', 'winui-sync', '--limit', '200', '--json', 'number,body') | ConvertFrom-Json
	$match = @($issues | Where-Object { "$($_.body)".Contains($marker) } | Sort-Object number)
	if ($match.Count -gt 0) { [int]$match[0].number } else { $null }
}

function Write-BodyFile {
	param([string]$Body)
	$file = Join-Path ([System.IO.Path]::GetTempPath()) "winui-sync-body-$([guid]::NewGuid().ToString('N')).md"
	[System.IO.File]::WriteAllText($file, $Body)
	$file
}

function New-GitHubIssue {
	param([string]$Title, [string]$Body)
	$url = Invoke-Gh @('issue', 'create', '--title', $Title, '--body-file', (Write-BodyFile $Body), '--label', 'winui-sync', '--label', 'kind/enhancement')
	[int](("$url".Trim() -split '/')[-1])
}

function Update-GitHubIssue {
	param([int]$Number, [string]$Title, [string]$Body)
	Invoke-Gh @('issue', 'edit', "$Number", '--title', $Title, '--body-file', (Write-BodyFile $Body)) | Out-Null
}

function New-GitHubPr {
	param([string]$Title, [string]$Body, [string]$Head, [string]$Base)
	$url = Invoke-Gh @('pr', 'create', '--draft', '--title', $Title, '--body-file', (Write-BodyFile $Body), '--head', $Head, '--base', $Base, '--label', 'winui-sync')
	[int](("$url".Trim() -split '/')[-1])
}

function Update-GitHubPr {
	param([int]$Number, [string]$Title, [string]$Body)
	Invoke-Gh @('pr', 'edit', "$Number", '--title', $Title, '--body-file', (Write-BodyFile $Body)) | Out-Null
}

function Add-GitHubPrComment {
	param([int]$Number, [string]$Body)
	Invoke-Gh @('pr', 'comment', "$Number", '--body-file', (Write-BodyFile $Body)) | Out-Null
}

function Push-SyncBranch {
	param([string]$RepoRoot, [string]$Branch, [switch]$ReplaceRemote)
	$pushArgs = @('push', '-q', 'origin', "HEAD:refs/heads/$Branch")
	if ($ReplaceRemote) { $pushArgs += '--force-with-lease' }
	Invoke-Git $RepoRoot $pushArgs | Out-Null
}

function Write-SyncSummary {
	param([string]$Line)
	if ($env:GITHUB_STEP_SUMMARY) { Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $Line }
	Write-Host $Line
}

# Applies one area's port result to its sync branch and creates or updates its issue and draft PR.
function Publish-SyncArea {
	param(
		[Parameter(Mandatory)][string]$RepoRoot,
		[Parameter(Mandatory)][string]$ManifestPath,
		[Parameter(Mandatory)][string]$Id,
		[Parameter(Mandatory)][string]$TargetTag,
		[Parameter(Mandatory)][string]$TargetSha,
		[Parameter(Mandatory)][string]$MasterSha,
		[Parameter(Mandatory)][string]$UpstreamRepo,
		[string]$PatchPath,
		[string]$ReportPath,
		[int]$PrNumber = 0,
		[bool]$CodeChanged = $false,
		[bool]$StringsChanged = $false,
		# 'ok' when the port job delivered its artifact; anything else means there is no patch to trust.
		[string]$PortStatus = 'ok',
		[string]$BaseBranch = 'master',
		[bool]$DryRun = $false)
	$branch = Get-SyncBranchName $Id
	$skip = {
		param($reason)
		Write-SyncSummary "- **$Id**: not published — $reason"
		if ($PrNumber -and -not $DryRun) { Add-GitHubPrComment -Number $PrNumber -Body "The WinUI sync run for ``$TargetTag`` could not update this PR: $reason" }
		[pscustomobject]@{ Outcome = "skipped: $reason"; IssueNumber = 0; PrNumber = $PrNumber }
	}

	if ($CodeChanged -and $PortStatus -ne 'ok') { return & $skip "the port job did not deliver a result ($PortStatus)." }
	if ((Initialize-SyncBranch -RepoRoot $RepoRoot -Id $Id -MasterSha $MasterSha -HasOpenPr:([bool]$PrNumber)) -eq 'conflict') {
		return & $skip "merging the default branch into ``$branch`` conflicts. Resolve the conflict on the branch; the next run continues from there."
	}
	$area = @(Get-SyncAreas -ManifestPath $ManifestPath | Where-Object id -EQ $Id)[0]
	if (-not $area) { return & $skip "area is not in the manifest on ``$branch``." }

	$report = [pscustomobject]@{ Valid = $true; BuildPassed = $true; SkippedCount = 0; Body = 'No upstream code changes in this range; translations only.' }
	if ($CodeChanged) {
		$report = Read-SyncReport -Path $ReportPath
		if (-not $report.Valid) { return & $skip 'the port did not produce a well-formed report, so what it covered is unknown. The next run redoes the port.' }
		if (-not (Invoke-ApplyPatch -RepoRoot $RepoRoot -PatchPath $PatchPath)) {
			return & $skip 'the ported patch no longer applies to the branch. The next run redoes the port.'
		}
		$refused = @(Test-SyncPatchPaths -Files @(Invoke-Git $RepoRoot @('diff', '--cached', '--name-only', '--no-renames', 'HEAD')))
		if ($refused.Count -gt 0) {
			Invoke-Git $RepoRoot @('reset', '-q', '--hard', 'HEAD') | Out-Null
			return & $skip "the ported patch touches files a port may not change: $(($refused | Select-Object -First 10) -join ', ')."
		}
	}

	$strings = $null
	if ($area.upstream.strings -and $area.uno.strings) {
		$strings = Sync-AreaStrings -UpstreamRepo $UpstreamRepo -Ref $TargetSha -UpstreamStrings $area.upstream.strings -UnoStrings (Join-Path $RepoRoot $area.uno.strings)
	}
	# From here on $report covers every run on this branch, not just this one.
	$report = Add-SyncRunReport -Path (Join-Path (Split-Path $ManifestPath) "reports/$Id.md") -Area $area -TargetTag $TargetTag -TargetSha $TargetSha -Report $report -Reset (-not $PrNumber)
	$issue = New-SyncIssue -Area $area -TargetTag $TargetTag -TargetSha $TargetSha -Report $report -StringsChanges $strings
	Set-AreaBaseline -ManifestPath $ManifestPath -Id $Id -Sha $TargetSha

	Invoke-Git $RepoRoot @('add', '-A') | Out-Null
	$changed = @(Invoke-Git $RepoRoot @('diff', '--cached', '--name-only', 'HEAD'))
	Invoke-Git $RepoRoot @('reset', '-q') | Out-Null
	$manifestRel = [System.IO.Path]::GetRelativePath((Resolve-Path -LiteralPath $RepoRoot).Path, (Resolve-Path -LiteralPath $ManifestPath).Path).Replace('\', '/')
	$identity = @('-c', 'user.name=github-actions[bot]', '-c', 'user.email=41898282+github-actions[bot]@users.noreply.github.com', '-c', 'commit.gpgsign=false')
	foreach ($group in Get-CommitGroups -Area $area -TargetTag $TargetTag -ChangedFiles $changed -ManifestRelativePath $manifestRel) {
		Invoke-Git $RepoRoot (@('add', '-A', '--') + $group.Files) | Out-Null
		Invoke-Git $RepoRoot ($identity + @('commit', '-q', '-m', $group.Message)) | Out-Null
	}

	if ($DryRun) {
		$pr = New-SyncPr -Area $area -TargetTag $TargetTag -Report $report -IssueNumber 0
		return [pscustomobject]@{ Outcome = 'dry-run'; IssueNumber = 0; PrNumber = $PrNumber; IssueTitle = $issue.Title; IssueBody = $issue.Body; PrTitle = $pr.Title; PrBody = $pr.Body }
	}

	Push-SyncBranch -RepoRoot $RepoRoot -Branch $branch -ReplaceRemote:(-not $PrNumber)

	$issueNumber = Find-SyncIssue -Id $Id
	if ($issueNumber) { Update-GitHubIssue -Number $issueNumber -Title $issue.Title -Body $issue.Body }
	else { $issueNumber = New-GitHubIssue -Title $issue.Title -Body $issue.Body }

	$pr = New-SyncPr -Area $area -TargetTag $TargetTag -Report $report -IssueNumber $issueNumber
	if ($PrNumber) { Update-GitHubPr -Number $PrNumber -Title $pr.Title -Body $pr.Body }
	else { $PrNumber = New-GitHubPr -Title $pr.Title -Body $pr.Body -Head $branch -Base $BaseBranch }

	Write-SyncSummary "- **$Id**: published — issue #$issueNumber, PR #$PrNumber ($($pr.Title))"
	[pscustomobject]@{ Outcome = 'published'; IssueNumber = $issueNumber; PrNumber = $PrNumber }
}

#endregion

Export-ModuleMember -Function *-*
