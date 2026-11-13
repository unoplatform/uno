BeforeAll {
	Import-Module "$PSScriptRoot/../WinUISync.psm1" -Force
	. "$PSScriptRoot/TestRepos.ps1"

	$script:Area = [pscustomobject]@{
		id       = 'foo'
		title    = 'Foo'
		baseline = ('a' * 40)
		upstream = [pscustomobject]@{ sources = @('controls/dev/Foo'); tests = @(); strings = 'controls/dev/Foo/Strings' }
		uno      = [pscustomobject]@{ sources = @('src/Foo'); tests = @('tests/Foo'); strings = 'src/Foo/Strings' }
	}

	function New-Report {
		param([bool]$BuildPassed = $true, [int]$Skipped = 0, [string]$Body = "## Ported`n- thing")
		[pscustomobject]@{ BuildPassed = $BuildPassed; SkippedCount = $Skipped; Body = $Body }
	}

	# Upstream (Foo v1 -> v2 tagged 1.1.0, one string changed), a bare Uno "origin", and a clone of it.
	function New-PublishFixture {
		$root = Join-Path $TestDrive "pub-$([guid]::NewGuid().ToString('N'))"
		$up = New-TestUpstream -Path "$root/up"
		$base = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = 'v1'; 'controls/dev/Foo/Strings/en-us/Resources.resw' = 'en v1' } -Tag 'winui3/release/1.0.0'
		$target = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = 'v2'; 'controls/dev/Foo/Strings/en-us/Resources.resw' = 'en v2' } -Tag 'winui3/release/1.1.0'

		$seed = New-TestRepo -Path "$root/seed"
		$area = New-TestArea -Id 'foo' -Baseline $base -Extra @{
			title    = 'Foo'
			upstream = [ordered]@{ sources = @('controls/dev/Foo'); strings = 'controls/dev/Foo/Strings' }
			uno      = [ordered]@{ sources = @('src/Foo'); tests = @('tests/Foo'); strings = 'src/Foo/Strings' }
		}
		$master = Add-TestCommit $seed @{
			'build/winui-sync/areas.json'          = (@{ areas = @($area) } | ConvertTo-Json -Depth 6)
			'src/Foo/Foo.cs'                       = "// MUX Reference Foo.cpp, commit $($base.Substring(0, 9))`nclass Foo { /* v1 */ }`n"
			'tests/Foo/FooTests.cs'                = "class FooTests { }`n"
			'src/Foo/Strings/en-US/Resources.resw' = 'en v1'
		}
		git init -q --bare -b main "$root/origin.git" | Out-Null
		Invoke-TestGit $seed push -q "$root/origin.git" main | Out-Null
		git clone -q "$root/origin.git" "$root/work" 2>&1 | Out-Null
		$work = (Resolve-Path "$root/work").Path
		foreach ($kv in @('user.email', 'test@example.invalid'), @('user.name', 'Test'), @('core.autocrlf', 'false'), @('commit.gpgsign', 'false')) {
			Invoke-TestGit $work config $kv[0] $kv[1] | Out-Null
		}

		# The patch an agent would have produced on top of master.
		[System.IO.File]::WriteAllText("$work/src/Foo/Foo.cs", "// MUX Reference Foo.cpp, tag winui3/release/1.1.0`nclass Foo { /* v2 */ }`n")
		[System.IO.File]::WriteAllText("$work/tests/Foo/FooTests.cs", "class FooTests { /* v2 */ }`n")
		$patch = "$root/sync.patch"
		Invoke-TestGit $work add -A | Out-Null
		Invoke-TestGit $work diff --cached --binary "--output=$patch" HEAD | Out-Null
		Invoke-TestGit $work reset -q --hard HEAD | Out-Null

		$report = "$root/sync-report.md"
		[System.IO.File]::WriteAllText($report, "build: passed`nskipped: 0`n---`n## Ported`n- Fix (#12086) by @alice`n")

		@{
			Root = $root; Upstream = $up; Base = $base; Target = $target; Origin = "$root/origin.git"; Work = $work
			Master = $master; Patch = $patch; Report = $report; Manifest = "$work/build/winui-sync/areas.json"
		}
	}

	function Invoke-Publish {
		param($Fx, [hashtable]$Override = @{})
		$params = @{
			RepoRoot = $Fx.Work; ManifestPath = $Fx.Manifest; Id = 'foo'; TargetTag = 'winui3/release/1.1.0'; TargetSha = $Fx.Target
			MasterSha = $Fx.Master; UpstreamRepo = $Fx.Upstream; PatchPath = $Fx.Patch; ReportPath = $Fx.Report
			CodeChanged = $true; StringsChanged = $true; BaseBranch = 'main'
		}
		foreach ($k in $Override.Keys) { $params[$k] = $Override[$k] }
		Publish-SyncArea @params
	}

	function Get-OriginBranchSha {
		param($Fx)
		@(git -C $Fx.Origin rev-parse --verify --quiet refs/heads/sync/winui/foo 2>$null)[0]
	}

	function Mock-GitHub {
		param([int]$ExistingIssue = 0)
		$found = if ($ExistingIssue) { $ExistingIssue } else { $null }
		Mock -ModuleName WinUISync Find-SyncIssue { $found }.GetNewClosure()
		Mock -ModuleName WinUISync New-GitHubIssue { 7 }
		Mock -ModuleName WinUISync Update-GitHubIssue { }
		Mock -ModuleName WinUISync New-GitHubPr { 99 }
		Mock -ModuleName WinUISync Update-GitHubPr { }
		Mock -ModuleName WinUISync Add-GitHubPrComment { }
	}
}

Describe 'ConvertTo-SafeReport' {
	It 'neutralises mentions' { ConvertTo-SafeReport 'thanks @alice' | Should -Be 'thanks `@alice`' }
	It 'neutralises team mentions' { ConvertTo-SafeReport 'cc @microsoft/winui' | Should -Be 'cc `@microsoft/winui`' }
	It 'leaves e-mail addresses alone' { ConvertTo-SafeReport 'a@b.com' | Should -Be 'a@b.com' }
	It 'qualifies bare refs' { ConvertTo-SafeReport 'Fix scroll (#12086)' | Should -Be 'Fix scroll (microsoft/microsoft-ui-xaml#12086)' }
	It 'qualifies GH- refs' { ConvertTo-SafeReport 'see GH-42' | Should -Be 'see microsoft/microsoft-ui-xaml#42' }
	It 'keeps qualified refs' { ConvertTo-SafeReport 'see unoplatform/uno#5' | Should -Be 'see unoplatform/uno#5' }
	It 'does not touch code spans' { ConvertTo-SafeReport '`@x` and `#1`' | Should -Be '`@x` and `#1`' }
	It 'does not touch fenced code' { ConvertTo-SafeReport "``````cpp`n#1 @x`n``````" | Should -Be "``````cpp`n#1 @x`n``````" }
	It 'does not touch markdown headings' { ConvertTo-SafeReport '## Ported' | Should -Be '## Ported' }
}

Describe 'Read-SyncReport' {
	It 'parses the front block and sanitises the body' {
		$p = Join-Path $TestDrive 'ok.md'
		Set-Content $p "build: passed`nskipped: 2`n---`n## Ported`nby @bob"
		$r = Read-SyncReport -Path $p
		$r.BuildPassed | Should -BeTrue
		$r.SkippedCount | Should -Be 2
		$r.Body | Should -Match '## Ported'
		$r.Body | Should -Match '`@bob`'
		$r.Body | Should -Not -Match 'build:'
	}

	It 'reads a failed build' {
		$p = Join-Path $TestDrive 'failed.md'
		Set-Content $p "build: failed`nskipped: 0`n---`nbody"
		(Read-SyncReport -Path $p).BuildPassed | Should -BeFalse
	}

	It 'fails closed on a missing file' {
		$r = Read-SyncReport -Path (Join-Path $TestDrive 'nope.md')
		$r.BuildPassed | Should -BeFalse
		$r.SkippedCount | Should -Be 1
		$r.Body | Should -Match 'report'
	}

	It 'fails closed on a malformed block and keeps the text' {
		$p = Join-Path $TestDrive 'bad.md'
		Set-Content $p "I ported everything, trust me.`nbuild: passed"
		$r = Read-SyncReport -Path $p
		$r.BuildPassed | Should -BeFalse
		$r.SkippedCount | Should -Be 1
		$r.Body | Should -Match 'trust me'
	}
}

Describe 'New-SyncIssue' {
	It 'has the marker, the range and the report' {
		$strings = [pscustomobject]@{ Added = @('de-DE/Resources.resw'); Removed = @(); Changed = @('en-US/Resources.resw') }
		$i = New-SyncIssue -Area $Area -TargetTag 'winui3/release/2.6.5' -TargetSha ('b' * 40) -Report (New-Report) -StringsChanges $strings
		$i.Title | Should -Be '[WinUI Sync] Foo → WinUI 2.6.5'
		$i.Body | Should -Match ([regex]::Escape('<!-- winui-sync:foo -->'))
		$i.Body | Should -Match 'aaaaaaaaaa'
		$i.Body | Should -Match 'bbbbbbbbbb'
		$i.Body | Should -Match '## Ported'
		$i.Body | Should -Match 'de-DE/Resources.resw'
	}

	It 'stays under the GitHub body limit' {
		$i = New-SyncIssue -Area $Area -TargetTag 'winui3/release/2.6.5' -TargetSha ('b' * 40) -Report (New-Report -Body ('x' * 200000)) -StringsChanges $null
		$i.Body.Length | Should -BeLessThan 65000
		$i.Body | Should -Match 'truncated'
	}
}

Describe 'New-SyncPr' {
	It 'closes the issue only when build passed and nothing skipped' {
		$pr = New-SyncPr -Area $Area -TargetTag 'winui3/release/2.6.5' -Report (New-Report) -IssueNumber 7
		$pr.Title | Should -Be 'fix(foo): Sync Foo with WinUI 2.6.5'
		($pr.Body -split "`n")[0] | Should -Be '**GitHub Issue:** closes #7'
	}

	It 'uses related-to when something was skipped' {
		$pr = New-SyncPr -Area $Area -TargetTag 'winui3/release/2.6.5' -Report (New-Report -Skipped 1) -IssueNumber 7
		($pr.Body -split "`n")[0] | Should -Be '**GitHub Issue:** related to #7'
		$pr.Title | Should -Not -Match 'build failing'
	}

	It 'uses related-to and flags the title when the build failed' {
		$pr = New-SyncPr -Area $Area -TargetTag 'winui3/release/2.6.5' -Report (New-Report -BuildPassed $false) -IssueNumber 7
		($pr.Body -split "`n")[0] | Should -Be '**GitHub Issue:** related to #7'
		$pr.Title | Should -Be '[build failing] fix(foo): Sync Foo with WinUI 2.6.5'
	}

	It 'carries the marker and the template sections' {
		$pr = New-SyncPr -Area $Area -TargetTag 'winui3/release/2.6.5' -Report (New-Report) -IssueNumber 7
		$pr.Body | Should -Match ([regex]::Escape('<!-- winui-sync:foo -->'))
		$pr.Body | Should -Match '## PR Type:'
		$pr.Body | Should -Match '## What changed\?'
		$pr.Body | Should -Match '## PR Checklist'
	}
}

Describe 'Get-CommitGroups' {
	It 'splits logic, tests, strings, manifest in that order and omits empty groups' {
		$files = @('build/winui-sync/areas.json', 'src/Foo/Strings/en-US/Resources.resw', 'tests/Foo/FooTests.cs', 'src/Foo/Foo.cs', 'src/Other/Dep.cs')
		$g = @(Get-CommitGroups -Area $Area -TargetTag 'winui3/release/2.6.5' -ChangedFiles $files -ManifestRelativePath 'build/winui-sync/areas.json')

		$g.Message | Should -Be @(
			'fix(foo): Sync Foo with WinUI 2.6.5',
			'test(foo): Sync Foo tests with WinUI 2.6.5',
			'chore(foo): Update Foo strings from WinUI 2.6.5',
			'chore(foo): Bump WinUI sync baseline to 2.6.5')
		$g[0].Files | Should -Be @('src/Foo/Foo.cs', 'src/Other/Dep.cs')
		$g[1].Files | Should -Be @('tests/Foo/FooTests.cs')

		$only = @(Get-CommitGroups -Area $Area -TargetTag 'winui3/release/2.6.5' -ChangedFiles @('build/winui-sync/areas.json') -ManifestRelativePath 'build/winui-sync/areas.json')
		$only.Count | Should -Be 1
	}
}

Describe 'Invoke-ApplyPatch' {
	It 'returns false and leaves a clean tree when the patch does not apply' {
		$fx = New-PublishFixture
		Add-TestCommit $fx.Work @{ 'src/Foo/Foo.cs' = "completely different`n" } | Out-Null

		Invoke-ApplyPatch -RepoRoot $fx.Work -PatchPath $fx.Patch | Should -BeFalse

		Invoke-TestGit $fx.Work status --porcelain | Should -BeNullOrEmpty
	}

	It 'applies a good patch into the index' {
		$fx = New-PublishFixture
		Invoke-ApplyPatch -RepoRoot $fx.Work -PatchPath $fx.Patch | Should -BeTrue
		@(Invoke-TestGit $fx.Work diff --cached --name-only) | Should -Be @('src/Foo/Foo.cs', 'tests/Foo/FooTests.cs')
	}

	It 'treats an empty patch as success' {
		$fx = New-PublishFixture
		$empty = Join-Path $fx.Root 'empty.patch'
		Set-Content $empty '' -NoNewline
		Invoke-ApplyPatch -RepoRoot $fx.Work -PatchPath $empty | Should -BeTrue
		Invoke-ApplyPatch -RepoRoot $fx.Work -PatchPath (Join-Path $fx.Root 'absent.patch') | Should -BeTrue
	}
}

Describe 'Initialize-SyncBranch' {
	It 'creates a fresh branch from master when there is no open PR' {
		$fx = New-PublishFixture
		Initialize-SyncBranch -RepoRoot $fx.Work -Id foo -MasterSha $fx.Master | Should -Be 'ready'
		Invoke-TestGit $fx.Work rev-parse --abbrev-ref HEAD | Should -Be 'sync/winui/foo'
		Invoke-TestGit $fx.Work rev-parse HEAD | Should -Be $fx.Master
	}

	It 'keeps a maintainer commit on the sync branch after merging master' {
		$fx = New-PublishFixture
		Invoke-TestGit $fx.Work checkout -q -b sync/winui/foo | Out-Null
		$maintainer = Add-TestCommit $fx.Work @{ 'src/Foo/Review.cs' = 'review fix' }
		Invoke-TestGit $fx.Work push -q origin sync/winui/foo | Out-Null
		Invoke-TestGit $fx.Work checkout -q main | Out-Null
		$newMaster = Add-TestCommit $fx.Work @{ 'README.md' = 'moved on' }
		git -C $fx.Work branch -q -D sync/winui/foo | Out-Null

		Initialize-SyncBranch -RepoRoot $fx.Work -Id foo -MasterSha $newMaster -HasOpenPr | Should -Be 'ready'

		Test-Path "$($fx.Work)/src/Foo/Review.cs" | Should -BeTrue
		Test-Path "$($fx.Work)/README.md" | Should -BeTrue
		Invoke-TestGit $fx.Work merge-base --is-ancestor $maintainer HEAD
	}

	It 'reports conflict and leaves no merge in progress' {
		$fx = New-PublishFixture
		Invoke-TestGit $fx.Work checkout -q -b sync/winui/foo | Out-Null
		Add-TestCommit $fx.Work @{ 'src/Foo/Foo.cs' = "branch side`n" } | Out-Null
		Invoke-TestGit $fx.Work push -q origin sync/winui/foo | Out-Null
		Invoke-TestGit $fx.Work checkout -q main | Out-Null
		$newMaster = Add-TestCommit $fx.Work @{ 'src/Foo/Foo.cs' = "master side`n" }

		Initialize-SyncBranch -RepoRoot $fx.Work -Id foo -MasterSha $newMaster -HasOpenPr | Should -Be 'conflict'

		Invoke-TestGit $fx.Work status --porcelain | Should -BeNullOrEmpty
		Test-Path "$($fx.Work)/.git/MERGE_HEAD" | Should -BeFalse
	}
}

Describe 'Publish-SyncArea' {
	It 'pushes grouped commits and opens an issue and a draft PR that closes it' {
		$fx = New-PublishFixture
		Mock-GitHub

		$r = Invoke-Publish $fx

		$r.Outcome | Should -Be 'published'
		$subjects = @(git -C $fx.Origin log --format=%s "$($fx.Master)..refs/heads/sync/winui/foo")
		[array]::Reverse($subjects)
		$subjects | Should -Be @(
			'fix(foo): Sync Foo with WinUI 1.1.0',
			'test(foo): Sync Foo tests with WinUI 1.1.0',
			'chore(foo): Update Foo strings from WinUI 1.1.0',
			'chore(foo): Bump WinUI sync baseline to 1.1.0')
		$manifest = (git -C $fx.Origin show 'refs/heads/sync/winui/foo:build/winui-sync/areas.json') -join "`n"
		@(Get-SyncAreasFromJson -Json $manifest)[0].baseline | Should -Be $fx.Target
		(git -C $fx.Origin show 'refs/heads/sync/winui/foo:src/Foo/Strings/en-US/Resources.resw') | Should -Be 'en v2'

		Should -Invoke -ModuleName WinUISync New-GitHubIssue -Times 1 -Exactly -ParameterFilter {
			$Title -eq '[WinUI Sync] Foo → WinUI 1.1.0' -and $Body -match 'microsoft/microsoft-ui-xaml#12086' -and $Body -match '`@alice`'
		}
		Should -Invoke -ModuleName WinUISync New-GitHubPr -Times 1 -Exactly -ParameterFilter {
			$Head -eq 'sync/winui/foo' -and $Base -eq 'main' -and $Body.StartsWith('**GitHub Issue:** closes #7')
		}
		Should -Invoke -ModuleName WinUISync Update-GitHubPr -Times 0 -Exactly
	}

	It 'empty patch with code changed still bumps the baseline and opens issue and PR' {
		$fx = New-PublishFixture
		Mock-GitHub
		Set-Content $fx.Patch '' -NoNewline

		$r = Invoke-Publish $fx @{ StringsChanged = $false }

		$r.Outcome | Should -Be 'published'
		$manifest = (git -C $fx.Origin show 'refs/heads/sync/winui/foo:build/winui-sync/areas.json') -join "`n"
		@(Get-SyncAreasFromJson -Json $manifest)[0].baseline | Should -Be $fx.Target
		Should -Invoke -ModuleName WinUISync New-GitHubIssue -Times 1 -Exactly
		Should -Invoke -ModuleName WinUISync New-GitHubPr -Times 1 -Exactly
	}

	It 'does not push or touch GitHub when the patch fails to apply' {
		$fx = New-PublishFixture
		Mock-GitHub
		Set-Content $fx.Patch "diff --git a/src/Foo/Foo.cs b/src/Foo/Foo.cs`n--- a/src/Foo/Foo.cs`n+++ b/src/Foo/Foo.cs`n@@ -1,1 +1,1 @@`n-not there`n+x`n"

		$r = Invoke-Publish $fx

		$r.Outcome | Should -Match '^skipped'
		Get-OriginBranchSha $fx | Should -BeNullOrEmpty
		Should -Invoke -ModuleName WinUISync New-GitHubIssue -Times 0 -Exactly
		Should -Invoke -ModuleName WinUISync New-GitHubPr -Times 0 -Exactly
	}

	It 'does not publish when the port job produced nothing for a code change' {
		$fx = New-PublishFixture
		Mock-GitHub

		$r = Invoke-Publish $fx @{ PortStatus = 'missing' }

		$r.Outcome | Should -Match '^skipped'
		Get-OriginBranchSha $fx | Should -BeNullOrEmpty
	}

	It 'updates the existing issue and PR and keeps maintainer commits' {
		$fx = New-PublishFixture
		Invoke-TestGit $fx.Work checkout -q -b sync/winui/foo | Out-Null
		$maintainer = Add-TestCommit $fx.Work @{ 'src/Foo/Review.cs' = 'review fix' }
		Invoke-TestGit $fx.Work push -q origin sync/winui/foo | Out-Null
		Invoke-TestGit $fx.Work checkout -q main | Out-Null
		git -C $fx.Work branch -q -D sync/winui/foo | Out-Null
		Mock-GitHub -ExistingIssue 7

		$r = Invoke-Publish $fx @{ PrNumber = 42 }

		$r.Outcome | Should -Be 'published'
		git -C $fx.Origin merge-base --is-ancestor $maintainer refs/heads/sync/winui/foo
		$LASTEXITCODE | Should -Be 0
		Should -Invoke -ModuleName WinUISync Update-GitHubIssue -Times 1 -Exactly -ParameterFilter { $Number -eq 7 }
		Should -Invoke -ModuleName WinUISync Update-GitHubPr -Times 1 -Exactly -ParameterFilter { $Number -eq 42 }
		Should -Invoke -ModuleName WinUISync New-GitHubIssue -Times 0 -Exactly
		Should -Invoke -ModuleName WinUISync New-GitHubPr -Times 0 -Exactly
	}

	It 'replaces a stale branch left by a closed PR' {
		$fx = New-PublishFixture
		Invoke-TestGit $fx.Work checkout -q -b sync/winui/foo | Out-Null
		$stale = Add-TestCommit $fx.Work @{ 'src/Foo/Stale.cs' = 'rejected work' }
		Invoke-TestGit $fx.Work push -q origin sync/winui/foo | Out-Null
		Invoke-TestGit $fx.Work checkout -q main | Out-Null
		git -C $fx.Work branch -q -D sync/winui/foo | Out-Null
		Mock-GitHub

		$r = Invoke-Publish $fx

		$r.Outcome | Should -Be 'published'
		git -C $fx.Origin merge-base --is-ancestor $stale refs/heads/sync/winui/foo
		$LASTEXITCODE | Should -Be 1
	}

	It 'comments on the open PR and pushes nothing on a merge conflict' {
		$fx = New-PublishFixture
		Invoke-TestGit $fx.Work checkout -q -b sync/winui/foo | Out-Null
		$branchSha = Add-TestCommit $fx.Work @{ 'src/Foo/Foo.cs' = "branch side`n" }
		Invoke-TestGit $fx.Work push -q origin sync/winui/foo | Out-Null
		Invoke-TestGit $fx.Work checkout -q main | Out-Null
		$newMaster = Add-TestCommit $fx.Work @{ 'src/Foo/Foo.cs' = "master side`n" }
		Mock-GitHub -ExistingIssue 7

		$r = Invoke-Publish $fx @{ PrNumber = 42; MasterSha = $newMaster }

		$r.Outcome | Should -Match '^skipped'
		Get-OriginBranchSha $fx | Should -Be $branchSha
		Should -Invoke -ModuleName WinUISync Add-GitHubPrComment -Times 1 -Exactly -ParameterFilter { $Number -eq 42 }
		Should -Invoke -ModuleName WinUISync Update-GitHubPr -Times 0 -Exactly
	}

	It 'dry run commits locally but pushes nothing and calls no GitHub API' {
		$fx = New-PublishFixture
		Mock-GitHub

		$r = Invoke-Publish $fx @{ DryRun = $true }

		$r.Outcome | Should -Be 'dry-run'
		$r.PrTitle | Should -Be 'fix(foo): Sync Foo with WinUI 1.1.0'
		Get-OriginBranchSha $fx | Should -BeNullOrEmpty
		@(Invoke-TestGit $fx.Work log --format=%s "$($fx.Master)..HEAD").Count | Should -Be 4
		Should -Invoke -ModuleName WinUISync Find-SyncIssue -Times 0 -Exactly
		Should -Invoke -ModuleName WinUISync New-GitHubIssue -Times 0 -Exactly
	}

	It 'strings-only change publishes without a patch or report' {
		$fx = New-PublishFixture
		Mock-GitHub

		$r = Invoke-Publish $fx @{ CodeChanged = $false; PatchPath = (Join-Path $fx.Root 'none.patch'); ReportPath = (Join-Path $fx.Root 'none.md'); PortStatus = 'missing' }

		$r.Outcome | Should -Be 'published'
		Should -Invoke -ModuleName WinUISync New-GitHubPr -Times 1 -Exactly -ParameterFilter { $Body.StartsWith('**GitHub Issue:** closes #7') }
	}
}
