BeforeAll {
	Import-Module "$PSScriptRoot/../WinUISync.psm1" -Force
	. "$PSScriptRoot/TestRepos.ps1"

	function New-UnoStrings {
		param([hashtable]$Files)
		$dir = Join-Path $TestDrive "uno-$([guid]::NewGuid())/Strings"
		foreach ($rel in $Files.Keys) {
			$full = Join-Path $dir $rel
			New-Item -ItemType Directory -Force -Path (Split-Path $full) | Out-Null
			[System.IO.File]::WriteAllText($full, $Files[$rel])
		}
		$dir
	}

	function Get-RelativeFiles {
		param([string]$Dir)
		@(Get-ChildItem -LiteralPath $Dir -Recurse -File | ForEach-Object { [System.IO.Path]::GetRelativePath($Dir, $_.FullName).Replace('\', '/') } | Sort-Object)
	}
}

Describe 'Sync-AreaMirror' {
	It 'replaces the Uno copy with upstream''s file, keeping its header line, BOM and line endings' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'mirror-up')
		$sha = Add-TestCommit $up @{ 'dxaml/themes/generic.xaml' = "<!-- Copyright -->`n<Root>v2</Root>`n<Tail />`n" } -Tag 'winui3/release/1.0.0'
		$uno = Join-Path $TestDrive 'mirror-uno'
		New-Item -ItemType Directory -Force -Path "$uno/src" | Out-Null
		[System.IO.File]::WriteAllText("$uno/src/Generic.xaml", "<!-- Copyright -->`r`n<!-- MUX Reference dxaml\themes\generic.xaml -->`r`n<Root>v1</Root>`r`n", [System.Text.UTF8Encoding]::new($true))
		$mirror = @([pscustomobject]@{ upstream = 'dxaml/themes/generic.xaml'; uno = 'src/Generic.xaml' })

		$changed = @(Sync-AreaMirror -UpstreamRepo $up -Ref $sha -UnoRoot $uno -Mirror $mirror)

		$changed | Should -Be @('src/Generic.xaml')
		[System.IO.File]::ReadAllText("$uno/src/Generic.xaml") | Should -Be "<!-- Copyright -->`r`n<!-- MUX Reference dxaml\themes\generic.xaml -->`r`n<Root>v2</Root>`r`n<Tail />`r`n"
		[System.IO.File]::ReadAllBytes("$uno/src/Generic.xaml")[0] | Should -Be 0xEF

		@(Sync-AreaMirror -UpstreamRepo $up -Ref $sha -UnoRoot $uno -Mirror $mirror).Count | Should -Be 0
	}

	It 'throws when the upstream file is missing, leaving the Uno copy alone' {
		$up = New-TestUpstream -Path (Join-Path $TestDrive 'mirror-up2')
		$sha = Add-TestCommit $up @{ 'other.txt' = 'x' }
		$uno = Join-Path $TestDrive 'mirror-uno2'
		New-Item -ItemType Directory -Force -Path "$uno/src" | Out-Null
		[System.IO.File]::WriteAllText("$uno/src/Generic.xaml", 'keep')
		$mirror = @([pscustomobject]@{ upstream = 'dxaml/themes/generic.xaml'; uno = 'src/Generic.xaml' })

		{ Sync-AreaMirror -UpstreamRepo $up -Ref $sha -UnoRoot $uno -Mirror $mirror } | Should -Throw '*dxaml/themes/generic.xaml*'
		[System.IO.File]::ReadAllText("$uno/src/Generic.xaml") | Should -Be 'keep'
	}
}

Describe 'Sync-AreaStrings' {
	BeforeEach {
		$script:up = New-TestUpstream -Path (Join-Path $TestDrive "up-$([guid]::NewGuid())")
		$script:ref = Add-TestCommit $up @{
			'controls/dev/Foo/Strings/en-us/Resources.resw' = 'en v2'
			'controls/dev/Foo/Strings/fr-FR/Resources.resw' = 'fr'
			'controls/dev/Foo/Strings/de-DE/Resources.resw' = 'de'
		}
	}

	It 'replaces content, adds new locales, removes locales upstream dropped' {
		$uno = New-UnoStrings @{ 'en-us/Resources.resw' = 'en v1'; 'fr-FR/Resources.resw' = 'fr'; 'xx-XX/Resources.resw' = 'gone' }

		$r = Sync-AreaStrings -UpstreamRepo $up -Ref $ref -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno

		$r.Changed | Should -Be @('en-us/Resources.resw')
		$r.Added | Should -Be @('de-DE/Resources.resw')
		$r.Removed | Should -Be @('xx-XX/Resources.resw')
		Get-RelativeFiles $uno | Should -Be @('de-DE/Resources.resw', 'en-us/Resources.resw', 'fr-FR/Resources.resw')
		Get-Content "$uno/en-us/Resources.resw" -Raw | Should -Be 'en v2'
		Test-Path "$uno/xx-XX" | Should -BeFalse
	}

	It 'leaves the Uno folder untouched and throws when upstream path is missing at ref' {
		$uno = New-UnoStrings @{ 'en-US/Resources.resw' = 'keep me' }

		{ Sync-AreaStrings -UpstreamRepo $up -Ref $ref -UpstreamStrings 'controls/dev/Renamed/Strings' -UnoStrings $uno } | Should -Throw '*not found at*'

		Get-Content "$uno/en-US/Resources.resw" -Raw | Should -Be 'keep me'
	}

	It 'reports nothing and changes nothing when already identical' {
		$uno = New-UnoStrings @{ 'en-us/Resources.resw' = 'en v2'; 'fr-FR/Resources.resw' = 'fr'; 'de-DE/Resources.resw' = 'de' }
		$stamp = (Get-Item "$uno/fr-FR/Resources.resw").LastWriteTimeUtc

		$r = Sync-AreaStrings -UpstreamRepo $up -Ref $ref -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno

		($r.Added.Count + $r.Removed.Count + $r.Changed.Count) | Should -Be 0
		(Get-Item "$uno/fr-FR/Resources.resw").LastWriteTimeUtc | Should -Be $stamp
	}

	It 'removes a Uno-only file inside an existing locale' {
		$uno = New-UnoStrings @{ 'en-us/Resources.resw' = 'en v2'; 'fr-FR/Resources.resw' = 'fr'; 'de-DE/Resources.resw' = 'de'; 'de-DE/Extra.resw' = 'uno only' }

		$r = Sync-AreaStrings -UpstreamRepo $up -Ref $ref -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno

		$r.Removed | Should -Be @('de-DE/Extra.resw')
		Test-Path "$uno/de-DE/Extra.resw" | Should -BeFalse
	}

	It 'keeps the folder casing Uno already has' {
		# Renaming en-US to en-us would rewrite every locale on each sync and breaks on case-insensitive checkouts.
		$uno = New-UnoStrings @{ 'en-US/Resources.resw' = 'en v1' }

		$r = Sync-AreaStrings -UpstreamRepo $up -Ref $ref -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno

		$r.Removed | Should -BeNullOrEmpty
		(Get-ChildItem -LiteralPath $uno -Directory | Where-Object Name -IEQ 'en-us').Name | Should -BeExactly 'en-US'
		Get-Content "$uno/en-US/Resources.resw" -Raw | Should -Be 'en v2'
	}

	It 'does not rewrite a file that differs only in line endings' {
		$crlf = Add-TestCommit $up @{ 'controls/dev/Foo/Strings/en-us/Resources.resw' = "a`r`nb" }
		$uno = New-UnoStrings @{ 'en-us/Resources.resw' = "a`nb"; 'fr-FR/Resources.resw' = 'fr'; 'de-DE/Resources.resw' = 'de' }

		$r = Sync-AreaStrings -UpstreamRepo $up -Ref $crlf -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno

		$r.Changed | Should -BeNullOrEmpty
		Get-Content "$uno/en-us/Resources.resw" -Raw | Should -Be "a`nb"
	}

	It 'finds the folder under src/ when upstream uses that layout' {
		$moved = Add-TestCommit $up @{
			'controls/dev/Foo/Strings/en-us/Resources.resw' = $null
			'controls/dev/Foo/Strings/fr-FR/Resources.resw' = $null
			'controls/dev/Foo/Strings/de-DE/Resources.resw' = $null
			'src/controls/dev/Foo/Strings/en-us/Resources.resw' = 'en v3'
		}
		$uno = New-UnoStrings @{ 'en-us/Resources.resw' = 'en v2' }

		Sync-AreaStrings -UpstreamRepo $up -Ref $moved -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno | Out-Null

		Get-Content "$uno/en-us/Resources.resw" -Raw | Should -Be 'en v3'
	}

	It 'creates the Uno folder when it does not exist yet' {
		$uno = Join-Path $TestDrive "fresh-$([guid]::NewGuid())/Strings"

		$r = Sync-AreaStrings -UpstreamRepo $up -Ref $ref -UpstreamStrings 'controls/dev/Foo/Strings' -UnoStrings $uno

		$r.Added.Count | Should -Be 3
		Test-Path "$uno/fr-FR/Resources.resw" | Should -BeTrue
	}
}
