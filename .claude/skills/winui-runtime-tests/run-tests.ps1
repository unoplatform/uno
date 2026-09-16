# run-tests.ps1 — Register the build output as a loose-layout package via winapp, run the
# runtime tests, and wait for the results.
#
# Usage:
#   pwsh -File run-tests.ps1 -ResultsFile <path> [-Filter <base64>] [-OutputDir <path>]
#                            [-TimeoutSeconds 600] [-DebugOutput] [-KeepRegistered]
#
# No MSIX packaging, signing or certificate is involved: winapp registers the build output
# folder directly (requires Developer Mode). The filter is a base64-encoded, pipe-separated
# list of fully qualified test names.

param(
	[Parameter(Mandatory = $true)]
	[string]$ResultsFile,

	[string]$Filter = "",

	# Defaults to the newest src\SamplesApp\SamplesApp\bin\x64\<config>\<tfm>\win-x64 holding an AppxManifest.xml
	[string]$OutputDir = "",

	[int]$TimeoutSeconds = 600,

	# Capture OutputDebugString + first-chance exceptions, and triage stowed exceptions on crash
	[switch]$DebugOutput,

	# Leave the development package registered (default: unregister once the app exits)
	[switch]$KeepRegistered
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

function Resolve-OutputDir {
	$binRoot = Join-Path $repoRoot "src\SamplesApp\SamplesApp\bin\x64"
	if (-not (Test-Path $binRoot)) {
		throw "No Windows build output under $binRoot. Build the app first (see SKILL.md Phase 2)."
	}
	# Packaging leaves staging copies (AppX\, ForBundle\) whose resources.pri is incomplete —
	# registering those fails with 0x80073B17. Only the folder holding the app exe is valid.
	$candidate = Get-ChildItem $binRoot -Recurse -Filter AppxManifest.xml -ErrorAction SilentlyContinue |
		Where-Object { $_.DirectoryName -notmatch '\\(AppX|ForBundle)$' } |
		Where-Object { Test-Path (Join-Path $_.DirectoryName "SamplesApp.Windows.exe") } |
		Sort-Object LastWriteTime -Descending | Select-Object -First 1
	if (-not $candidate) {
		throw "No build output with both AppxManifest.xml and SamplesApp.Windows.exe under $binRoot. Did the build succeed?"
	}
	return $candidate.Directory.FullName
}

function Resolve-Winapp {
	if ($env:WINAPP_EXE -and (Test-Path $env:WINAPP_EXE)) {
		return $env:WINAPP_EXE
	}
	$onPath = Get-Command winapp -ErrorAction SilentlyContinue
	if ($onPath) {
		return $onPath.Source
	}
	# The Microsoft.Windows.SDK.BuildTools.WinApp package (referenced by the head) bundles winapp.exe,
	# so no global install is required.
	$packages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE ".nuget\packages" }
	$arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
	$bundled = Get-ChildItem (Join-Path $packages "microsoft.windows.sdk.buildtools.winapp") -Directory -ErrorAction SilentlyContinue |
		Sort-Object Name -Descending |
		ForEach-Object { Join-Path $_.FullName "tools\$arch\winapp.exe" } |
		Where-Object { Test-Path $_ } | Select-Object -First 1
	if ($bundled) {
		return $bundled
	}
	throw "winapp.exe not found. Install it (winget install Microsoft.WinAppCli), restore the SamplesApp head, or set WINAPP_EXE."
}

# The app is framework-dependent: if the machine's PATH dotnet has no matching shared runtime
# (net11 previews live side-by-side), point DOTNET_ROOT at the install that does have it.
function Set-DotnetRootIfNeeded([string]$outputDir) {
	if ($env:DOTNET_ROOT) {
		return
	}
	$runtimeConfig = Get-ChildItem $outputDir -Filter "*.runtimeconfig.json" | Select-Object -First 1
	if (-not $runtimeConfig) {
		return
	}
	$version = (Get-Content $runtimeConfig.FullName -Raw | ConvertFrom-Json).runtimeOptions.framework.version
	if (-not $version) {
		return
	}
	$sharedName = ($version -split '-')[0]
	foreach ($root in @("$env:ProgramFiles\dotnet", "$env:LOCALAPPDATA\Microsoft\dotnet")) {
		if (Test-Path (Join-Path $root "shared\Microsoft.NETCore.App\$version")) {
			if ($root -ne "$env:ProgramFiles\dotnet") {
				$env:DOTNET_ROOT = $root
				Write-Host "DOTNET_ROOT set to $root (runtime $sharedName)"
			}
			return
		}
	}
	Write-Warning "No installed .NET runtime $version found — the app may fail to start."
}

$ResultsFile = [System.IO.Path]::GetFullPath($ResultsFile)
if (-not $OutputDir) {
	$OutputDir = Resolve-OutputDir
}
$OutputDir = (Resolve-Path $OutputDir).Path
$manifestPath = Join-Path $OutputDir "AppxManifest.xml"
$winapp = Resolve-Winapp

Write-Host "winapp:     $winapp"
Write-Host "Output dir: $OutputDir"

# winapp refuses to replace a package installed from an MSIX (non-development mode).
Get-AppxPackage -Name 'SamplesApp' -ErrorAction SilentlyContinue |
	Where-Object { -not $_.IsDevelopmentMode } |
	ForEach-Object {
		Write-Host "Removing conflicting MSIX-installed package: $($_.PackageFullName)"
		Remove-AppxPackage -Package $_.PackageFullName
	}

Set-DotnetRootIfNeeded $OutputDir

Remove-Item $ResultsFile, "$ResultsFile.canary" -Force -ErrorAction SilentlyContinue

$winappArgs = @('run', $OutputDir, '--manifest', $manifestPath, '--with-alias')
if (-not $KeepRegistered) {
	$winappArgs += '--unregister-on-exit'
}
if ($DebugOutput) {
	$winappArgs += '--debug-output'
}
$winappArgs += '--'
$winappArgs += "--runtime-tests=$ResultsFile"
if ($Filter) {
	$winappArgs += "--runtime-test-filter=$Filter"
}

Write-Host "Launching: winapp $($winappArgs -join ' ')"
$sw = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $winapp -ArgumentList $winappArgs -PassThru -NoNewWindow

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
	Write-Host "Timeout after $TimeoutSeconds seconds — stopping the app."
	Get-Process -Name 'SamplesApp.Windows' -ErrorAction SilentlyContinue | Stop-Process -Force
	Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
	& $winapp unregister --manifest $manifestPath --force | Out-Null
	throw "Test run timed out after $TimeoutSeconds seconds."
}

Write-Host ""
Write-Host "winapp exited with code $($process.ExitCode) after $($sw.Elapsed)."

if (-not (Test-Path $ResultsFile)) {
	if (Test-Path "$ResultsFile.canary") {
		throw "The app started (canary written) but produced no results file — it likely crashed mid-run. Re-run with -DebugOutput for a stack trace."
	}
	throw "No results file at $ResultsFile — the app never reached the runtime-test entry point."
}

Write-Host "Results: $ResultsFile"
