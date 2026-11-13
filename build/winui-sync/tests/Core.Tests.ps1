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

Describe 'Expand-SyncPrompt' {
	BeforeAll {
		$script:up = New-TestUpstream -Path (Join-Path $TestDrive 'prompt-up')
		$script:base = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = 'v1'; 'controls/dev/Foo/Strings/en-us/Resources.resw' = 'en' }
		$script:target = Add-TestCommit $up @{
			'controls/dev/Foo/Foo.cpp'                          = $null
			'controls/dev/Foo/Strings/en-us/Resources.resw'     = $null
			'src/controls/dev/Foo/Foo.cpp'                      = 'v2'
			'src/controls/dev/Foo/Strings/en-us/Resources.resw' = 'en'
		} -Tag 'winui3/release/1.1.0'
		$m = Write-TestManifest (Join-Path $TestDrive 'prompt.json') @(
			(New-TestArea -Id 'foo' -Baseline $base -Extra @{
				title    = 'Foo'
				upstream = [ordered]@{ sources = @('controls/dev/Foo'); strings = 'controls/dev/Foo/Strings' }
				uno      = [ordered]@{ sources = @('src/Uno.UI/Foo'); tests = @('src/Tests/Foo'); strings = 'src/Uno.UI/Foo/Strings' }
			}))
		$script:area = @(Get-SyncAreas -ManifestPath $m)[0]
	}

	It 'fills the real template completely' {
		$text = Expand-SyncPrompt -TemplatePath "$PSScriptRoot/../prompt.md" -Area $area -TargetTag 'winui3/release/1.1.0' -TargetSha $target -UpstreamRepo $up -UpstreamDir '.winui-upstream'

		$text | Should -Not -Match '\{\{'
		$text | Should -Match 'Foo'
		$text | Should -Match ([regex]::Escape('winui3/release/1.1.0'))
		$text | Should -Match ([regex]::Escape("git -C .winui-upstream diff ${base}:controls/dev/Foo ${target}:src/controls/dev/Foo -- . ':(exclude)Strings'"))
		$text | Should -Match ([regex]::Escape('src/Uno.UI/Foo'))
		$text | Should -Match ([regex]::Escape('src/Tests/Foo'))
		$text | Should -Match 'sync-report\.md'
		$text | Should -Match 'build: passed\|failed'
		foreach ($project in [regex]::Matches($text, 'dotnet build (\S+\.csproj)') | ForEach-Object { $_.Groups[1].Value }) {
			Test-Path (Join-Path "$PSScriptRoot/../../.." $project) | Should -BeTrue -Because "$project is named in the prompt"
		}
	}

	It 'throws on an unknown placeholder' {
		$template = Join-Path $TestDrive 'bad-prompt.md'
		Set-Content $template 'Port {{title}} using {{foo}}.'
		{ Expand-SyncPrompt -TemplatePath $template -Area $area -TargetTag 'winui3/release/1.1.0' -TargetSha $target -UpstreamRepo $up -UpstreamDir 'u' } | Should -Throw '*{{foo}}*'
	}
}
