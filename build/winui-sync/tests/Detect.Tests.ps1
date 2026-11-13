BeforeAll {
	Import-Module "$PSScriptRoot/../WinUISync.psm1" -Force
	. "$PSScriptRoot/TestRepos.ps1"

	# Upstream with one control at v1 (tagged 1.0.0); returns @{ Repo; Base }.
	function New-ControlUpstream {
		param([string]$Name)
		$up = New-TestUpstream -Path (Join-Path $TestDrive $Name)
		$base = Add-TestCommit $up @{
			'controls/dev/Foo/Foo.cpp'                      = 'v1'
			'controls/dev/Foo/APITests/FooTests.cpp'        = 't1'
			'controls/dev/Foo/Strings/en-us/Resources.resw' = "<root>`r`n  en`r`n</root>"
			'controls/dev/Bar/Bar.cpp'                      = 'v1'
		} -Tag 'winui3/release/1.0.0'
		@{ Repo = $up; Base = $base }
	}

	function New-FooArea {
		param([string]$Baseline, [string]$Id = 'foo')
		$m = Write-TestManifest (Join-Path $TestDrive "$([guid]::NewGuid()).json") @(
			(New-TestArea -Id $Id -Baseline $Baseline -Extra @{
				upstream = [ordered]@{ sources = @('controls/dev/Foo'); tests = @('controls/dev/Foo/APITests'); strings = 'controls/dev/Foo/Strings' }
				uno      = [ordered]@{ sources = @('src/Foo'); tests = @('tests/Foo'); strings = 'src/Foo/Strings' }
			}))
		@(Get-SyncAreas -ManifestPath $m)[0]
	}

	# Uno working tree whose Strings folder matches the upstream fixture.
	function New-UnoTree {
		param([string]$Name, [string]$Locale = 'en-US')
		$uno = Join-Path $TestDrive $Name
		New-Item -ItemType Directory -Force -Path "$uno/src/Foo/Strings/$Locale" | Out-Null
		[System.IO.File]::WriteAllText("$uno/src/Foo/Strings/$Locale/Resources.resw", "<root>`n  en`n</root>")
		$uno
	}
}

Describe 'Get-AreaSyncState' {
	BeforeEach {
		$script:fx = New-ControlUpstream "up-$([guid]::NewGuid())"
		$script:uno = New-UnoTree "uno-$([guid]::NewGuid())"
		$script:area = New-FooArea -Baseline $fx.Base
	}

	It 'codeChanged is false when only files outside the area changed' {
		$target = Add-TestCommit $fx.Repo @{ 'controls/dev/Bar/Bar.cpp' = 'v2' }
		$s = Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno
		$s.codeChanged | Should -BeFalse
		$s.stringsChanged | Should -BeFalse
	}

	It 'codeChanged is true for a new upstream file in the area' {
		$target = Add-TestCommit $fx.Repo @{ 'controls/dev/Foo/FooHelper.cpp' = 'new' }
		(Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno).codeChanged | Should -BeTrue
	}

	It 'codeChanged is true for a deleted upstream file in the area' {
		$target = Add-TestCommit $fx.Repo @{ 'controls/dev/Foo/APITests/FooTests.cpp' = $null }
		(Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno).codeChanged | Should -BeTrue
	}

	It 'a strings-only upstream change sets stringsChanged, not codeChanged' {
		$target = Add-TestCommit $fx.Repo @{ 'controls/dev/Foo/Strings/en-us/Resources.resw' = '<root>changed</root>' }
		$s = Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno
		$s.codeChanged | Should -BeFalse
		$s.stringsChanged | Should -BeTrue
	}

	It 'stringsChanged is true when Uno has an extra locale folder' {
		New-Item -ItemType Directory -Force -Path "$uno/src/Foo/Strings/xx-XX" | Out-Null
		Set-Content "$uno/src/Foo/Strings/xx-XX/Resources.resw" 'extra'
		(Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $fx.Base -UnoRoot $uno).stringsChanged | Should -BeTrue
	}

	It 'stringsChanged ignores path case and line endings' {
		# Fixture: upstream en-us + CRLF, Uno en-US + LF.
		(Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $fx.Base -UnoRoot $uno).stringsChanged | Should -BeFalse
	}

	It 'throws when upstream strings path is missing at target' {
		$target = Add-TestCommit $fx.Repo @{ 'controls/dev/Foo/Strings/en-us/Resources.resw' = $null }
		{ Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno } | Should -Throw '*not found at*'
	}

	It 'sees no change when upstream only moved the area under src/' {
		$target = Add-TestCommit $fx.Repo @{
			'controls/dev/Foo/Foo.cpp'                          = $null
			'controls/dev/Foo/APITests/FooTests.cpp'            = $null
			'controls/dev/Foo/Strings/en-us/Resources.resw'     = $null
			'src/controls/dev/Foo/Foo.cpp'                      = 'v1'
			'src/controls/dev/Foo/APITests/FooTests.cpp'        = 't1'
			'src/controls/dev/Foo/Strings/en-us/Resources.resw' = "<root>`r`n  en`r`n</root>"
		}
		$s = Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno
		$s.codeChanged | Should -BeFalse
		$s.stringsChanged | Should -BeFalse
	}

	It 'sees a content change across the src/ layout move' {
		$target = Add-TestCommit $fx.Repo @{
			'controls/dev/Foo/Foo.cpp'                          = $null
			'controls/dev/Foo/APITests/FooTests.cpp'            = $null
			'controls/dev/Foo/Strings/en-us/Resources.resw'     = $null
			'src/controls/dev/Foo/Foo.cpp'                      = 'v2'
			'src/controls/dev/Foo/APITests/FooTests.cpp'        = 't1'
			'src/controls/dev/Foo/Strings/en-us/Resources.resw' = "<root>`r`n  en`r`n</root>"
		}
		$s = Get-AreaSyncState -Area $area -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno
		$s.codeChanged | Should -BeTrue
		$s.diffStat | Should -Match '1 file changed'
	}

	It 'treats a source path that only exists at the target as changed' {
		$m = Write-TestManifest (Join-Path $TestDrive "$([guid]::NewGuid()).json") @(
			(New-TestArea -Id 'foo' -Baseline $fx.Base -Extra @{ upstream = [ordered]@{ sources = @('controls/dev/Foo', 'controls/dev/New') } }))
		$twoPaths = @(Get-SyncAreas -ManifestPath $m)[0]
		$target = Add-TestCommit $fx.Repo @{ 'controls/dev/New/New.cpp' = 'n' }
		(Get-AreaSyncState -Area $twoPaths -UpstreamRepo $fx.Repo -TargetSha $target -UnoRoot $uno).codeChanged | Should -BeTrue
	}

	It 'throws when a manifest path exists at neither commit' {
		$m = Write-TestManifest (Join-Path $TestDrive "$([guid]::NewGuid()).json") @(
			(New-TestArea -Id 'foo' -Baseline $fx.Base -Extra @{ upstream = [ordered]@{ sources = @('controls/dev/Foo', 'controls/dev/Typo') } }))
		$typo = @(Get-SyncAreas -ManifestPath $m)[0]
		{ Get-AreaSyncState -Area $typo -UpstreamRepo $fx.Repo -TargetSha $fx.Base -UnoRoot $uno } | Should -Throw '*controls/dev/Typo*'
	}

	It 'throws on a baseline that is not in the clone' {
		$bad = New-FooArea -Baseline ('d' * 40)
		{ Get-AreaSyncState -Area $bad -UpstreamRepo $fx.Repo -TargetSha $fx.Base -UnoRoot $uno } | Should -Throw '*baseline*'
	}
}

Describe 'Select-SyncMatrix' {
	It 'orders by oldest baseline and caps at MaxAreas, reporting the deferred ids' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'order')
		$c1 = Add-TestCommit $up @{ 'a' = '1' }
		$c2 = Add-TestCommit $up @{ 'a' = '2' }
		$c3 = Add-TestCommit $up @{ 'a' = '3' }
		$areas = @(
			[pscustomobject]@{ id = 'newest'; baseline = $c3 },
			[pscustomobject]@{ id = 'oldest'; baseline = $c1 },
			[pscustomobject]@{ id = 'middle'; baseline = $c2 },
			[pscustomobject]@{ id = 'quiet'; baseline = $c1 })
		$states = @(
			[pscustomobject]@{ id = 'newest'; codeChanged = $true; stringsChanged = $false },
			[pscustomobject]@{ id = 'oldest'; codeChanged = $false; stringsChanged = $true },
			[pscustomobject]@{ id = 'middle'; codeChanged = $true; stringsChanged = $true },
			[pscustomobject]@{ id = 'quiet'; codeChanged = $false; stringsChanged = $false })

		$r = Select-SyncMatrix -States $states -Areas $areas -UpstreamRepo $up -MaxAreas 2

		$r.Selected | Should -Be @('oldest', 'middle')
		$r.Deferred | Should -Be @('newest')
	}
}

Describe 'Get-SyncPlan' {
	BeforeEach {
		$script:fx = New-ControlUpstream "plan-up-$([guid]::NewGuid())"
		$script:target = Add-TestCommit $fx.Repo @{ 'controls/dev/Foo/Foo.cpp' = 'v2' } -Tag 'winui3/release/1.1.0'

		# Uno side is a real repo here: the plan reads the manifest and master sha from it.
		$script:uno = New-TestRepo -Path (Join-Path $TestDrive "plan-uno-$([guid]::NewGuid())")
		$script:manifestRel = 'build/winui-sync/areas.json'
		$area = New-TestArea -Id 'foo' -Baseline $fx.Base -Extra @{
			upstream = [ordered]@{ sources = @('controls/dev/Foo'); strings = 'controls/dev/Foo/Strings' }
			uno      = [ordered]@{ sources = @('src/Foo'); strings = 'src/Foo/Strings' }
		}
		$script:areaDef = $area
		$json = (@{ areas = @($area) } | ConvertTo-Json -Depth 6)
		$script:master = Add-TestCommit $uno @{
			$manifestRel                             = $json
			'src/Foo/Strings/en-US/Resources.resw' = "<root>`n  en`n</root>"
		}
	}

	It 'puts a changed area in the matrix with defaults filled in' {
		Mock -ModuleName WinUISync Get-OpenSyncPr { $null }

		$plan = Get-SyncPlan -UpstreamRepo $fx.Repo -UnoRoot $uno -ManifestPath (Join-Path $uno $manifestRel)

		$plan.TargetTag | Should -Be 'winui3/release/1.1.0'
		$plan.TargetSha | Should -Be $target
		$plan.MasterSha | Should -Be $master
		$plan.Matrix.Count | Should -Be 1
		$item = $plan.Matrix[0]
		$item.id | Should -Be 'foo'
		$item.baseline | Should -Be $fx.Base
		$item.codeChanged | Should -BeTrue
		$item.stringsChanged | Should -BeFalse
		$item.branch | Should -Be 'sync/winui/foo'
		$item.model | Should -Be 'claude-opus-5-5'
		$item.prNumber | Should -Be 0
	}

	It 'uses the baseline from the open sync branch' {
		Mock -ModuleName WinUISync Get-OpenSyncPr { 42 }
		$areaDef.baseline = $target
		Invoke-TestGit $uno checkout -q -b work | Out-Null
		$branchSha = Add-TestCommit $uno @{ $manifestRel = (@{ areas = @($areaDef) } | ConvertTo-Json -Depth 6) }
		Invoke-TestGit $uno update-ref refs/remotes/origin/sync/winui/foo $branchSha | Out-Null
		Invoke-TestGit $uno checkout -q main | Out-Null

		$plan = Get-SyncPlan -UpstreamRepo $fx.Repo -UnoRoot $uno -ManifestPath (Join-Path $uno $manifestRel)

		$plan.Matrix.Count | Should -Be 0
		$plan.Rows[0].status | Should -Be 'up to date'
		$plan.Rows[0].prNumber | Should -Be 42
	}

	It 'returns an empty matrix when nothing changed' {
		Mock -ModuleName WinUISync Get-OpenSyncPr { $null }

		$plan = Get-SyncPlan -UpstreamRepo $fx.Repo -UnoRoot $uno -ManifestPath (Join-Path $uno $manifestRel) -TargetTag 'winui3/release/1.0.0'

		$plan.Matrix.Count | Should -Be 0
	}

	It 'reports a broken area and still plans the others' {
		Mock -ModuleName WinUISync Get-OpenSyncPr { $null }
		$bad = New-TestArea -Id 'bad' -Baseline ('d' * 40) -Extra @{ upstream = [ordered]@{ sources = @('controls/dev/Foo') } }
		Add-TestCommit $uno @{ $manifestRel = (@{ areas = @($bad, $areaDef) } | ConvertTo-Json -Depth 6) } | Out-Null

		$plan = Get-SyncPlan -UpstreamRepo $fx.Repo -UnoRoot $uno -ManifestPath (Join-Path $uno $manifestRel)

		$plan.Matrix.id | Should -Be @('foo')
		($plan.Rows | Where-Object id -EQ 'bad').status | Should -Match '^error: '
		@($plan.Errors).Count | Should -Be 1
	}

	It 'honours the area filter' {
		Mock -ModuleName WinUISync Get-OpenSyncPr { $null }

		$plan = Get-SyncPlan -UpstreamRepo $fx.Repo -UnoRoot $uno -ManifestPath (Join-Path $uno $manifestRel) -AreaFilter 'other'

		$plan.Matrix.Count | Should -Be 0
		$plan.Rows.Count | Should -Be 0
	}
}

Describe 'detect.ps1' {
	It 'emits has_work=false and an empty matrix when nothing changed' {
		$fx = New-ControlUpstream 'detect-up'
		$uno = New-TestRepo -Path (Join-Path $TestDrive 'detect-uno')
		$area = New-TestArea -Id 'foo' -Baseline $fx.Base -Extra @{ upstream = [ordered]@{ sources = @('controls/dev/Foo') } }
		Add-TestCommit $uno @{ 'build/winui-sync/areas.json' = (@{ areas = @($area) } | ConvertTo-Json -Depth 6) } | Out-Null
		$out = Join-Path $TestDrive 'gh-output.txt'
		$summary = Join-Path $TestDrive 'gh-summary.md'

		$env:GITHUB_OUTPUT = $out
		$env:GITHUB_STEP_SUMMARY = $summary
		Mock -ModuleName WinUISync Get-OpenSyncPr { $null }
		try {
			& "$PSScriptRoot/../detect.ps1" -UpstreamRepo $fx.Repo -UnoRoot $uno -ManifestPath (Join-Path $uno 'build/winui-sync/areas.json') | Out-Null
		}
		finally {
			Remove-Item Env:GITHUB_OUTPUT, Env:GITHUB_STEP_SUMMARY
		}

		$lines = Get-Content $out
		$lines | Should -Contain 'has_work=false'
		$lines | Should -Contain 'has_errors=false'
		$lines | Should -Contain 'matrix=[]'
		$lines | Should -Contain 'target_tag=winui3/release/1.0.0'
		(Get-Content $summary -Raw) | Should -Match 'foo'
	}
}

Describe 'Select-OpenSyncPr' {
	It 'ignores pull requests opened from forks' {
		$json = '[{"number":5,"isCrossRepository":true},{"number":9,"isCrossRepository":false}]'
		Select-OpenSyncPr -Json $json | Should -Be 9
	}

	It 'returns nothing when only a fork PR uses the branch name' {
		Select-OpenSyncPr -Json '[{"number":5,"isCrossRepository":true}]' | Should -BeNullOrEmpty
		Select-OpenSyncPr -Json '[]' | Should -BeNullOrEmpty
	}
}
