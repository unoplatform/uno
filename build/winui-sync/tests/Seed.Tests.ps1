BeforeAll {
	Import-Module "$PSScriptRoot/../WinUISync.psm1" -Force
	. "$PSScriptRoot/TestRepos.ps1"

	function New-CsFile {
		param([string]$Path, [string]$Content)
		New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
		[System.IO.File]::WriteAllText($Path, $Content)
	}
}

Describe 'Get-MuxReferences' {
	It 'parses commit, full sha, tag and missing refs' {
		$root = Join-Path $TestDrive 'uno1'
		New-CsFile "$root/A.cs" "// MUX Reference TabView.cpp, commit 65718e2813`nclass A {}"
		New-CsFile "$root/B.cs" "// MUX Reference: X.cpp, tag winui3/release/1.8.4`nclass B {}"
		New-CsFile "$root/C.cs" "// MUX Reference dxaml\foo.cpp`nclass C {}"
		New-CsFile "$root/D.cs" "class D {}"
		New-CsFile "$root/sub/E.cs" "// MUX reference SelectorBar.cpp, commit: b91b3ce6f25c587a9e18c4e122f348f51331f18b."
		New-CsFile "$root/F.txt" "// MUX Reference TabView.cpp, commit 1234567"

		$refs = @(Get-MuxReferences -Paths $root)
		$byFile = @{}
		foreach ($r in $refs) { $byFile[(Split-Path $r.File -Leaf)] = $r }

		$refs.Count | Should -Be 5
		$byFile['A.cs'].Kind | Should -Be 'commit'
		$byFile['A.cs'].Ref | Should -Be '65718e2813'
		$byFile['B.cs'].Kind | Should -Be 'tag'
		$byFile['B.cs'].Ref | Should -Be 'winui3/release/1.8.4'
		$byFile['C.cs'].Kind | Should -Be 'none'
		$byFile['D.cs'].Kind | Should -Be 'none'
		$byFile['E.cs'].Kind | Should -Be 'commit'
		$byFile['E.cs'].Ref | Should -Be 'b91b3ce6f25c587a9e18c4e122f348f51331f18b'
	}

	It 'returns every distinct ref of a file that has several' {
		$root = Join-Path $TestDrive 'uno2'
		New-CsFile "$root/A.cs" "// MUX Reference A.cpp, commit 1111111`n// MUX Reference A.h, commit 2222222`n// MUX Reference A.idl, commit 1111111"

		$refs = @(Get-MuxReferences -Paths $root)

		($refs.Ref | Sort-Object) | Should -Be @('1111111', '2222222')
	}
}

Describe 'Resolve-AreaBaseline' {
	BeforeAll {
		$script:up = New-TestUpstream -Path (Join-Path $TestDrive 'up')
		$script:c1 = Add-TestCommit $up @{ 'a.txt' = '1' }
		$script:c2 = Add-TestCommit $up @{ 'a.txt' = '2' } -Tag 'winui3/release/1.8.4'
		function New-Ref($kind, $ref, $file = 'X.cs') { [pscustomobject]@{ File = $file; Kind = $kind; Ref = $ref } }
	}

	It 'picks the oldest ancestor when refs are mixed' {
		$r = Resolve-AreaBaseline -UpstreamRepo $up -References @(
			(New-Ref 'tag' 'winui3/release/1.8.4'),
			(New-Ref 'commit' $c1.Substring(0, 9)))

		$r.Baseline | Should -Be $c1
		$r.Resolved['winui3/release/1.8.4'] | Should -Be $c2
		$r.Unresolved | Should -BeNullOrEmpty
	}

	It 'reports an unresolvable sha and still proposes a baseline' {
		$r = Resolve-AreaBaseline -UpstreamRepo $up -References @(
			(New-Ref 'commit' $c1.Substring(0, 9)),
			(New-Ref 'commit' 'deadbeef1'))

		$r.Unresolved | Should -Be @('deadbeef1')
		$r.Baseline | Should -Be $c1
	}

	It 'returns null baseline when nothing resolves' {
		$r = Resolve-AreaBaseline -UpstreamRepo $up -References @((New-Ref 'commit' 'deadbeef1'), (New-Ref 'none' $null 'Y.cs'))

		$r.Baseline | Should -BeNullOrEmpty
		$r.NoRef | Should -Be @('Y.cs')
	}

	It 'falls back to the oldest commit date on diverged history' {
		Invoke-TestGit $up checkout -q -b side $c1 | Out-Null
		$side = Add-TestCommit $up @{ 'b.txt' = 'side' }
		Invoke-TestGit $up checkout -q main | Out-Null
		$main = Add-TestCommit $up @{ 'a.txt' = '3' }

		$r = Resolve-AreaBaseline -UpstreamRepo $up -References @((New-Ref 'commit' $main), (New-Ref 'commit' $side))

		$r.Baseline | Should -Be $side
	}
}

Describe 'seed-area.ps1' {
	It 'appends a valid entry with -Write' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'up2')
		$c1 = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = '1'; 'controls/dev/Foo/Strings/en-us/Resources.resw' = 'en' } -Tag 'winui3/release/1.0.0'
		$uno = Join-Path $TestDrive 'uno3'
		New-CsFile "$uno/src/Foo/Foo.cs" "// MUX Reference Foo.cpp, commit $($c1.Substring(0, 10))"
		$manifest = Write-TestManifest (Join-Path $uno 'areas.json') @()

		& "$PSScriptRoot/../seed-area.ps1" -Id foo -Title Foo -UpstreamRepo $up -UnoRoot $uno -ManifestPath $manifest `
			-UpstreamSources 'controls/dev/Foo' -UnoSources 'src/Foo' -UnoStrings 'src/Foo/Strings' -UpstreamStrings 'controls/dev/Foo/Strings' -Write | Out-Null

		$areas = @(Get-SyncAreas -ManifestPath $manifest)
		$areas[0].id | Should -Be 'foo'
		$areas[0].baseline | Should -Be $c1
		$areas[0].uno.strings | Should -Be 'src/Foo/Strings'
	}

	It 'splits comma-joined path arguments and rejects a path upstream does not have' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'up4')
		$c1 = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = '1'; 'controls/dev/Foo/APITests/T.cpp' = '1'; 'controls/dev/Foo/InteractionTests/T.cpp' = '1' } -Tag 'winui3/release/1.0.0'
		$uno = Join-Path $TestDrive 'uno4'
		New-CsFile "$uno/src/Foo/Foo.cs" "// MUX Reference Foo.cpp, commit $($c1.Substring(0, 10))"
		$manifest = Write-TestManifest (Join-Path $uno 'areas.json') @()
		$common = @{ Id = 'foo'; Title = 'Foo'; UpstreamRepo = $up; UnoRoot = $uno; ManifestPath = $manifest; UpstreamSources = 'controls/dev/Foo'; UnoSources = 'src/Foo'; Write = $true }

		& "$PSScriptRoot/../seed-area.ps1" @common -UpstreamTests 'controls/dev/Foo/APITests,controls/dev/Foo/InteractionTests' | Out-Null
		@(Get-SyncAreas -ManifestPath $manifest)[0].upstream.tests | Should -Be @('controls/dev/Foo/APITests', 'controls/dev/Foo/InteractionTests')

		{ & "$PSScriptRoot/../seed-area.ps1" @common -UpstreamTests 'controls/dev/Foo/Nope' } | Should -Throw '*controls/dev/Foo/Nope*'
	}

	It 'takes an explicit baseline over the one derived from headers' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'up5')
		$old = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = '1' }
		$new = Add-TestCommit $up @{ 'controls/dev/Foo/Foo.cpp' = '2' } -Tag 'winui3/release/1.0.0'
		$uno = Join-Path $TestDrive 'uno5'
		New-CsFile "$uno/src/Foo/Foo.cs" "// MUX Reference Foo.cpp, commit $($new.Substring(0, 10))"
		New-CsFile "$uno/src/Foo/Stale.cs" "// MUX Reference Foo.cpp, commit $($old.Substring(0, 10))"
		$manifest = Write-TestManifest (Join-Path $uno 'areas.json') @()
		$common = @{ Id = 'foo'; Title = 'Foo'; UpstreamRepo = $up; UnoRoot = $uno; ManifestPath = $manifest; UpstreamSources = 'controls/dev/Foo'; UnoSources = 'src/Foo'; Write = $true }

		& "$PSScriptRoot/../seed-area.ps1" @common -Baseline $new.Substring(0, 10) | Out-Null
		@(Get-SyncAreas -ManifestPath $manifest)[0].baseline | Should -Be $new

		{ & "$PSScriptRoot/../seed-area.ps1" @common -Baseline 'deadbeef1' } | Should -Throw '*deadbeef1*'
	}
}
