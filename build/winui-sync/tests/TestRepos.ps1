# Git fixtures for the WinUI sync tests.

$script:TestCommitCounter = 0

function Invoke-TestGit {
	param([Parameter(Mandatory, Position = 0)][string]$Repo, [Parameter(ValueFromRemainingArguments)][string[]]$GitArgs)
	$all = & git -C $Repo @GitArgs 2>&1
	if ($LASTEXITCODE -ne 0) { throw "git $($GitArgs -join ' ') failed: $all" }
	$all | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" }
}

function New-TestRepo {
	param([Parameter(Mandatory)][string]$Path)
	New-Item -ItemType Directory -Force -Path $Path | Out-Null
	Invoke-TestGit $Path init -q -b main | Out-Null
	Invoke-TestGit $Path config user.email 'test@example.invalid' | Out-Null
	Invoke-TestGit $Path config user.name 'Test' | Out-Null
	Invoke-TestGit $Path config core.autocrlf false | Out-Null
	Invoke-TestGit $Path config commit.gpgsign false | Out-Null
	Invoke-TestGit $Path config tag.gpgsign false | Out-Null
	(Resolve-Path $Path).Path
}

function New-TestUpstream {
	param([Parameter(Mandatory)][string]$Path)
	New-TestRepo -Path $Path
}

# $Files: relative path -> content; a $null value deletes the file. Returns the full commit SHA.
function Add-TestCommit {
	param(
		[Parameter(Mandatory, Position = 0)][string]$Repo,
		[Parameter(Mandatory, Position = 1)][hashtable]$Files,
		[string]$Tag,
		[string]$Message = 'commit')
	foreach ($rel in $Files.Keys) {
		$full = Join-Path $Repo $rel
		if ($null -eq $Files[$rel]) {
			Remove-Item -Force $full
		}
		else {
			New-Item -ItemType Directory -Force -Path (Split-Path $full) | Out-Null
			[System.IO.File]::WriteAllText($full, [string]$Files[$rel])
		}
	}
	# Distinct, increasing dates keep date-ordered assertions deterministic.
	$script:TestCommitCounter++
	$date = ([datetime]'2020-01-01T00:00:00Z').AddMinutes($script:TestCommitCounter).ToString('yyyy-MM-ddTHH:mm:ssZ')
	$env:GIT_AUTHOR_DATE = $date
	$env:GIT_COMMITTER_DATE = $date
	try {
		Invoke-TestGit $Repo add -A | Out-Null
		Invoke-TestGit $Repo commit -q --allow-empty -m $Message | Out-Null
	}
	finally {
		Remove-Item Env:GIT_AUTHOR_DATE, Env:GIT_COMMITTER_DATE -ErrorAction SilentlyContinue
	}
	if ($Tag) { Invoke-TestGit $Repo tag $Tag | Out-Null }
	(Invoke-TestGit $Repo rev-parse HEAD)
}

function Write-TestManifest {
	param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Areas)
	New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
	[System.IO.File]::WriteAllText($Path, ((@{ areas = @($Areas) } | ConvertTo-Json -Depth 6) + "`n"))
	$Path
}

function New-TestArea {
	param([string]$Id = 'tabview', [string]$Baseline = ('a' * 40), [hashtable]$Extra = @{})
	$area = [ordered]@{
		id       = $Id
		title    = $Id
		baseline = $Baseline
		upstream = [ordered]@{ sources = @("controls/dev/$Id") }
		uno      = [ordered]@{ sources = @("src/$Id") }
	}
	foreach ($k in $Extra.Keys) { $area[$k] = $Extra[$k] }
	$area
}
