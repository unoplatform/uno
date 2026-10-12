# dotnet-root.ps1 — Dot-sourced by run-tests.ps1 and run-tests-msix.ps1.
#
# The app is framework-dependent and net11 previews are usually installed side-by-side, so
# DOTNET_ROOT_<ARCH> must point at an install that has the exact runtime the app was built
# against, in the app's own architecture (an x64 app on ARM64 needs the emulated x64 install).

# Returns 'x64', 'arm64' or 'x86' from a PE file's machine field, or $null.
function Get-PeArchitecture([string]$path) {
	if (-not (Test-Path $path)) {
		return $null
	}
	$stream = [System.IO.File]::OpenRead($path)
	try {
		$reader = [System.IO.BinaryReader]::new($stream)
		$stream.Position = 0x3C
		$stream.Position = $reader.ReadInt32() + 4
		switch ($reader.ReadUInt16()) {
			0x8664 { return 'x64' }
			0xAA64 { return 'arm64' }
			0x014C { return 'x86' }
			default { return $null }
		}
	}
	finally {
		$stream.Dispose()
	}
}

function Set-DotnetRootForApp([string]$appDir) {
	$runtimeConfig = Get-ChildItem $appDir -Filter "*.runtimeconfig.json" -ErrorAction SilentlyContinue | Select-Object -First 1
	if (-not $runtimeConfig) {
		Write-Warning "No *.runtimeconfig.json in $appDir; DOTNET_ROOT left unchanged."
		return
	}

	$options = (Get-Content $runtimeConfig.FullName -Raw | ConvertFrom-Json).runtimeOptions
	$framework = @($options.framework) + @($options.frameworks) |
		Where-Object { $_ -and $_.name -eq 'Microsoft.NETCore.App' } |
		Select-Object -First 1
	if (-not $framework.version) {
		return
	}
	$version = $framework.version

	$osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
	$appExe = Join-Path $appDir ($runtimeConfig.Name -replace '\.runtimeconfig\.json$', '.exe')
	$appArch = Get-PeArchitecture $appExe
	if (-not $appArch) {
		Write-Warning "Cannot read the architecture of $appExe; assuming $osArch."
		$appArch = $osArch
	}
	$archVariable = "DOTNET_ROOT_$($appArch.ToUpperInvariant())"

	# Default global install locations for the app's architecture on this OS.
	$globalRoot = switch ($appArch) {
		{ $_ -eq $osArch } { "$env:ProgramFiles\dotnet"; break }
		'x64' { "$env:ProgramFiles\dotnet\x64"; break }
		'x86' { "${env:ProgramFiles(x86)}\dotnet"; break }
	}

	# Inherited values are only kept when they hold the runtime in the right architecture.
	$candidates = @((Get-Item "env:$archVariable" -ErrorAction SilentlyContinue).Value, $env:DOTNET_ROOT, $globalRoot, "$env:LOCALAPPDATA\Microsoft\dotnet") |
		Where-Object { $_ } | Select-Object -Unique
	foreach ($root in $candidates) {
		$coreclr = Join-Path $root "shared\Microsoft.NETCore.App\$version\coreclr.dll"
		if ((Get-PeArchitecture $coreclr) -eq $appArch) {
			# The apphost prefers DOTNET_ROOT_<ARCH> over DOTNET_ROOT, so a wrong-architecture
			# DOTNET_ROOT inherited from the shell cannot win.
			if ((Get-Item "env:$archVariable" -ErrorAction SilentlyContinue).Value -ne $root) {
				Set-Item "env:$archVariable" $root
				Write-Host "$archVariable set to $root (runtime $version)"
			}
			return
		}
	}
	Write-Warning "No installed $appArch .NET runtime $version found (searched $($candidates -join ', ')); the app may fail to start."
}
