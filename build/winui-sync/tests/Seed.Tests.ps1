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

	It 'reads XAML headers in folders and skips XAML without one' {
		$root = Join-Path $TestDrive 'uno-xaml'
		New-CsFile "$root/Foo.xaml" "<!-- MUX Reference src\controls\dev\Foo\Foo.xaml, tag winui3/release/2.5.1 -->`n<ResourceDictionary />"
		New-CsFile "$root/TestPage.xaml" '<Page />'

		$refs = @(Get-MuxReferences -Paths $root)

		$refs.Count | Should -Be 1
		$refs[0].Ref | Should -Be 'winui3/release/2.5.1'
	}
}

Describe 'Remove-MuxReferenceVersion' {
	It 'drops tags and commits from header lines, keeping files, encoding and line endings' {
		$root = Join-Path $TestDrive 'strip'
		New-Item -ItemType Directory -Force -Path "$root/sub" | Out-Null
		$bom = [System.Text.UTF8Encoding]::new($true)
		[System.IO.File]::WriteAllText("$root/A.cs", "// Copyright`r`n// MUX Reference A.cpp, tag winui3/release/1.4.2, commit ba3a8d59e`r`n// A_Partial.h, A_Partial.cpp, tag winui3/release/1.4.2.`r`nclass A { } // commit to this, tag along`r`n", $bom)
		[System.IO.File]::WriteAllText("$root/sub/B.xaml", "<!-- MUX Reference src\controls\dev\B\B.xaml, tag winui3/release/2.5.1, commit ba3a8d59e -->`n<ResourceDictionary />`n")
		[System.IO.File]::WriteAllText("$root/C.cs", "// MUX Reference: C.cpp, commit 65718e2813`nclass C { }`n")
		[System.IO.File]::WriteAllText("$root/D.cs", "// MUX Reference D.cpp`nclass D { }`n")

		$changed = @(Remove-MuxReferenceVersion -Paths $root)

		($changed | ForEach-Object { Split-Path $_ -Leaf } | Sort-Object) | Should -Be @('A.cs', 'B.xaml', 'C.cs')
		[System.IO.File]::ReadAllText("$root/A.cs") | Should -Be "// Copyright`r`n// MUX Reference A.cpp`r`n// A_Partial.h, A_Partial.cpp`r`nclass A { } // commit to this, tag along`r`n"
		[System.IO.File]::ReadAllBytes("$root/A.cs")[0] | Should -Be 0xEF
		[System.IO.File]::ReadAllText("$root/sub/B.xaml") | Should -Be "<!-- MUX Reference src\controls\dev\B\B.xaml -->`n<ResourceDictionary />`n"
		[System.IO.File]::ReadAllBytes("$root/sub/B.xaml")[0] | Should -Be ([byte][char]'<')
		[System.IO.File]::ReadAllText("$root/C.cs") | Should -Be "// MUX Reference: C.cpp`nclass C { }`n"
	}

	It 'leaves a header without a version, and code after the header, alone' {
		$root = Join-Path $TestDrive 'strip-none'
		New-Item -ItemType Directory -Force -Path $root | Out-Null
		$text = "// MUX Reference E.cpp`nclass E { }`n" + ("`n" * 20) + "// tag winui3/release/1.0.0`n"
		[System.IO.File]::WriteAllText("$root/E.cs", $text)

		@(Remove-MuxReferenceVersion -Paths $root).Count | Should -Be 0
		[System.IO.File]::ReadAllText("$root/E.cs") | Should -Be $text
	}
}

Describe 'Find-AreaThemeXaml' {
	BeforeAll {
		$script:up = New-TestUpstream -Path (Join-Path $TestDrive 'theme-up')
		$script:tip = Add-TestCommit $up @{
			'controls/dev/Foo/Foo.cpp'                                       = '1'
			'controls/dev/Foo/Foo.xaml'                                      = '<Style />'
			'controls/dev/Foo/Foo_themeresources.xaml'                       = '<Res />'
			'controls/dev/Foo/TestUI/FooPage.xaml'                           = '<Page />'
			'controls/dev/CommonStyles/HyperlinkButton_themeresources.xaml'  = '<Res />'
			'controls/dev/CommonStyles/HyperlinkButton_themeresources_perf2026.xaml' = '<Res />'
			'controls/dev/CommonStyles/Bar_themeresources.xaml'              = '<Res />'
			'controls/dev/CommonStyles/Foo2_Bar_themeresources.xaml'         = '<Res />'
			'dxaml/lib/Foo2_Bar_Partial.cpp'                                 = '1'
			'dxaml/lib/HyperLinkButton_Partial.cpp'                          = '1'
			'dxaml/lib/HyperlinkButtonAutomationPeer_Partial.cpp'            = '1'
		} -Tag 'winui3/release/1.0.0'
		$script:uno = Join-Path $TestDrive 'theme-uno'
		$theme = "$uno/src/Uno.UI.FluentTheme/Resources/PriorityDefault"
		New-CsFile "$theme/Foo.xaml" "<!-- MUX Reference src\controls\dev\Foo\Foo.xaml, tag winui3/release/1.0.0 -->`n<ResourceDictionary />"
		New-CsFile "$theme/Foo_themeresources.xaml" '<ResourceDictionary />'
		New-CsFile "$theme/FooPage.xaml" '<ResourceDictionary />'
		New-CsFile "$theme/HyperlinkButton_themeresources.xaml" "<!-- MUX Reference src\controls\dev\CommonStyles\HyperlinkButton_themeresources.xaml, tag winui3/release/1.0.0 -->`n<ResourceDictionary />"
		New-CsFile "$theme/Bar_themeresources.xaml" '<ResourceDictionary />'
		New-CsFile "$theme/Other.xaml" "<!-- MUX Reference src\controls\dev\Other\Other.xaml, tag winui3/release/1.0.0 -->`n<ResourceDictionary />"
	}

	It 'finds a folder area''s styles by header and by name, but not its test pages' {
		$found = @(Find-AreaThemeXaml -UpstreamRepo $up -Ref $tip -UnoRoot $uno -UpstreamSources 'controls/dev/Foo')

		($found.Uno | Sort-Object) | Should -Be (@(
				'src/Uno.UI.FluentTheme/Resources/PriorityDefault/Foo.xaml',
				'src/Uno.UI.FluentTheme/Resources/PriorityDefault/Foo_themeresources.xaml') | Sort-Object)
		$found.Upstream | Where-Object { $_ } | Should -BeNullOrEmpty
	}

	It 'finds a core control''s CommonStyles file, ignoring case and perf variants' {
		$found = @(Find-AreaThemeXaml -UpstreamRepo $up -Ref $tip -UnoRoot $uno -UpstreamSources 'dxaml/lib/HyperLinkButton_Partial.cpp', 'dxaml/lib/HyperlinkButtonAutomationPeer_Partial.cpp')

		$found.Count | Should -Be 1
		$found[0].Uno | Should -Be 'src/Uno.UI.FluentTheme/Resources/PriorityDefault/HyperlinkButton_themeresources.xaml'
		$found[0].Upstream | Should -Be 'controls/dev/CommonStyles/HyperlinkButton_themeresources.xaml'
	}

	It 'maps control names with digits and underscores' {
		New-CsFile "$uno/src/Uno.UI.FluentTheme/Resources/PriorityDefault/Foo2_Bar_themeresources.xaml" '<ResourceDictionary />'

		$found = @(Find-AreaThemeXaml -UpstreamRepo $up -Ref $tip -UnoRoot $uno -UpstreamSources 'dxaml/lib/Foo2_Bar_Partial.cpp')

		$found.Count | Should -Be 1
		$found[0].Upstream | Should -Be 'controls/dev/CommonStyles/Foo2_Bar_themeresources.xaml'
	}

	It 'returns nothing when the Uno tree has no FluentTheme' {
		@(Find-AreaThemeXaml -UpstreamRepo $up -Ref $tip -UnoRoot (Join-Path $TestDrive 'no-theme') -UpstreamSources 'controls/dev/Foo').Count | Should -Be 0
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

	It 'accepts individual files on both sides' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'up6')
		$c1 = Add-TestCommit $up @{ 'dxaml/lib/Button.cpp' = '1'; 'dxaml/lib/Slider.cpp' = '1' } -Tag 'winui3/release/1.0.0'
		$uno = Join-Path $TestDrive 'uno6'
		New-CsFile "$uno/src/Controls/Button.cs" "// MUX Reference Button.cpp, commit $($c1.Substring(0, 10))"
		New-CsFile "$uno/src/Controls/Slider.cs" "// MUX Reference Slider.cpp, commit 1234567"
		$manifest = Write-TestManifest (Join-Path $uno 'areas.json') @()
		$common = @{ Id = 'button'; Title = 'Button'; UpstreamRepo = $up; UnoRoot = $uno; ManifestPath = $manifest; UnoSources = 'src/Controls/Button.cs'; Write = $true }

		$out = & "$PSScriptRoot/../seed-area.ps1" @common -UpstreamSources 'dxaml/lib/Button.cpp'
		$area = @(Get-SyncAreas -ManifestPath $manifest)[0]
		$area.baseline | Should -Be $c1
		$area.upstream.sources | Should -Be @('dxaml/lib/Button.cpp')
		($out -join "`n") | Should -Not -Match '1234567'

		{ & "$PSScriptRoot/../seed-area.ps1" @common -UpstreamSources 'dxaml/lib/Nope.cpp' } | Should -Throw '*dxaml/lib/Nope.cpp*'
	}

	It 'adds the control''s theme XAML to the entry unless told not to' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'up7')
		$c1 = Add-TestCommit $up @{
			'dxaml/lib/Bar_Partial.cpp'                        = '1'
			'controls/dev/CommonStyles/Bar_themeresources.xaml' = '<Res />'
		} -Tag 'winui3/release/1.0.0'
		$uno = Join-Path $TestDrive 'uno7'
		New-CsFile "$uno/src/Controls/Bar.cs" "// MUX Reference Bar_Partial.cpp, commit $($c1.Substring(0, 10))"
		New-CsFile "$uno/src/Uno.UI.FluentTheme/Resources/PriorityDefault/Bar_themeresources.xaml" "<!-- MUX Reference src\controls\dev\CommonStyles\Bar_themeresources.xaml, tag winui3/release/1.0.0 -->`n<ResourceDictionary />"
		$manifest = Write-TestManifest (Join-Path $uno 'areas.json') @()
		$common = @{ Id = 'bar'; Title = 'Bar'; UpstreamRepo = $up; UnoRoot = $uno; ManifestPath = $manifest; UpstreamSources = 'dxaml/lib/Bar_Partial.cpp'; UnoSources = 'src/Controls/Bar.cs'; Write = $true }

		& "$PSScriptRoot/../seed-area.ps1" @common | Out-Null
		$area = @(Get-SyncAreas -ManifestPath $manifest)[0]
		$area.upstream.sources | Should -Be @('dxaml/lib/Bar_Partial.cpp', 'controls/dev/CommonStyles/Bar_themeresources.xaml')
		$area.uno.sources | Should -Be @('src/Controls/Bar.cs', 'src/Uno.UI.FluentTheme/Resources/PriorityDefault/Bar_themeresources.xaml')

		& "$PSScriptRoot/../seed-area.ps1" @common -NoThemeXaml | Out-Null
		$area = @(Get-SyncAreas -ManifestPath $manifest)[0]
		$area.upstream.sources | Should -Be @('dxaml/lib/Bar_Partial.cpp')
		$area.uno.sources | Should -Be @('src/Controls/Bar.cs')
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
