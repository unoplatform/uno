BeforeAll {
	Import-Module "$PSScriptRoot/../WinUISync.psm1" -Force
	. "$PSScriptRoot/TestRepos.ps1"
}

Describe 'Get-LatestStableTag' {
	It 'picks highest version, ignoring suffixed tags' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'up')
		Add-TestCommit $up @{ 'a.txt' = '1' } -Tag 'winui3/release/2.9.1' | Out-Null
		$sha = Add-TestCommit $up @{ 'a.txt' = '2' } -Tag 'winui3/release/2.10.0'
		Add-TestCommit $up @{ 'a.txt' = '3' } -Tag 'winui3/release/2.10.1-experimental' | Out-Null
		Add-TestCommit $up @{ 'a.txt' = '4' } -Tag 'winui3/release/1.8.12' | Out-Null

		$r = Get-LatestStableTag -UpstreamRepo $up

		$r.Tag | Should -Be 'winui3/release/2.10.0'
		$r.Version | Should -Be '2.10.0'
		$r.Sha | Should -Be $sha
	}

	It 'returns the commit, not the tag object, for annotated tags' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'annotated')
		$sha = Add-TestCommit $up @{ 'a.txt' = '1' }
		Invoke-TestGit $up tag -a 'winui3/release/1.0.0' -m 'release' | Out-Null

		(Get-LatestStableTag -UpstreamRepo $up).Sha | Should -Be $sha
	}

	It 'throws when there is no stable tag' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'empty')
		Add-TestCommit $up @{ 'a.txt' = '1' } -Tag 'winui3/release/1.0.0-experimental' | Out-Null

		{ Get-LatestStableTag -UpstreamRepo $up } | Should -Throw '*no stable*'
	}
}

Describe 'Get-SyncAreas' {
	It 'rejects duplicate ids' {
		$m = Write-TestManifest (Join-Path $TestDrive 'dup.json') @((New-TestArea), (New-TestArea))
		{ Get-SyncAreas -ManifestPath $m } | Should -Throw '*duplicate*'
	}

	It 'rejects abbreviated baseline' {
		$m = Write-TestManifest (Join-Path $TestDrive 'short.json') @((New-TestArea -Baseline 'abc1234'))
		{ Get-SyncAreas -ManifestPath $m } | Should -Throw '*baseline*'
	}

	It 'rejects an area without upstream sources' {
		$area = New-TestArea
		$area.upstream = [ordered]@{ sources = @() }
		$m = Write-TestManifest (Join-Path $TestDrive 'nosrc.json') @($area)
		{ Get-SyncAreas -ManifestPath $m } | Should -Throw '*upstream.sources*'
	}

	It 'defaults optional fields' {
		$m = Write-TestManifest (Join-Path $TestDrive 'min.json') @((New-TestArea))
		$areas = @(Get-SyncAreas -ManifestPath $m)

		$areas.Count | Should -Be 1
		$areas[0].upstream.tests | Should -BeNullOrEmpty
		$areas[0].upstream.strings | Should -BeNullOrEmpty
		$areas[0].uno.tests | Should -BeNullOrEmpty
		$areas[0].model | Should -BeNullOrEmpty
		$areas[0].upstream.sources | Should -Be @('controls/dev/tabview')
	}

	It 'returns an empty list for an empty manifest' {
		$m = Write-TestManifest (Join-Path $TestDrive 'none.json') @()
		@(Get-SyncAreas -ManifestPath $m).Count | Should -Be 0
	}
}

Describe 'Set-AreaBaseline' {
	It 'changes only the named entry' {
		$m = Write-TestManifest (Join-Path $TestDrive 'two.json') @(
			(New-TestArea -Id 'one' -Baseline ('a' * 40)),
			(New-TestArea -Id 'two' -Baseline ('b' * 40)))

		Set-AreaBaseline -ManifestPath $m -Id 'two' -Sha ('c' * 40)

		$areas = @(Get-SyncAreas -ManifestPath $m)
		($areas | Where-Object id -EQ 'one').baseline | Should -Be ('a' * 40)
		($areas | Where-Object id -EQ 'two').baseline | Should -Be ('c' * 40)
	}

	It 'is stable when the baseline is unchanged' {
		$m = Write-TestManifest (Join-Path $TestDrive 'stable.json') @((New-TestArea -Id 'one' -Baseline ('a' * 40)))
		Set-AreaBaseline -ManifestPath $m -Id 'one' -Sha ('a' * 40)
		$before = Get-Content $m -Raw
		Set-AreaBaseline -ManifestPath $m -Id 'one' -Sha ('a' * 40)
		Get-Content $m -Raw | Should -Be $before
	}

	It 'throws for an unknown id' {
		$m = Write-TestManifest (Join-Path $TestDrive 'unk.json') @((New-TestArea -Id 'one'))
		{ Set-AreaBaseline -ManifestPath $m -Id 'nope' -Sha ('c' * 40) } | Should -Throw '*nope*'
	}
}
