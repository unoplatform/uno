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

	# Defaults to the newest src\SamplesApp\SamplesApp\bin\x64\Release\<tfm>\win-x64 holding an AppxManifest.xml
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
	# build-app.ps1 only produces Release; a newer Debug output must not be picked up instead.
	$binRoot = Join-Path $repoRoot "src\SamplesApp\SamplesApp\bin\x64\Release"
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

# winapp provisions the Windows App Runtime for its own process architecture, so it has to match
# the build output's RID (build-app.ps1 produces win-x64, which also runs emulated on ARM64).
function Resolve-Winapp([string]$rid) {
	if ($env:WINAPP_EXE -and (Test-Path $env:WINAPP_EXE)) {
		return $env:WINAPP_EXE
	}
	# The Microsoft.Windows.SDK.BuildTools.WinApp package (referenced by the head) bundles winapp.exe
	# per architecture, so no global install is required.
	$packages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE ".nuget\packages" }
	$bundled = Get-ChildItem (Join-Path $packages "microsoft.windows.sdk.buildtools.winapp") -Directory -ErrorAction SilentlyContinue |
		Sort-Object { $v = $null; if ([version]::TryParse(($_.Name -split '-')[0], [ref]$v)) { $v } else { [version]'0.0' } } -Descending |
		ForEach-Object { Join-Path $_.FullName "tools\$rid\winapp.exe" } |
		Where-Object { Test-Path $_ } | Select-Object -First 1
	if ($bundled) {
		return $bundled
	}
	$onPath = Get-Command winapp -ErrorAction SilentlyContinue
	if ($onPath) {
		$hostRid = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
		if ($hostRid -ne $rid) {
			Write-Warning "Using winapp from PATH, which is likely $hostRid while the build output is $rid. Restore the SamplesApp head or set WINAPP_EXE to a $rid winapp.exe."
		}
		return $onPath.Source
	}
	throw "winapp.exe not found. Restore the SamplesApp head, install it (winget install Microsoft.WinAppCli), or set WINAPP_EXE."
}

# Start-Process joins -ArgumentList without quoting, so paths with spaces would be split.
function Format-PathArgument([string]$value) {
	return "`"$value`""
}

. (Join-Path $PSScriptRoot "dotnet-root.ps1")

$ResultsFile = [System.IO.Path]::GetFullPath($ResultsFile)
# The app joins its arguments with '&' and splits them again (App.Tests.ParseArgs), so the path would be truncated.
if ($ResultsFile.Contains('&')) {
	throw "ResultsFile must not contain '&' (the app splits its arguments on it): $ResultsFile"
}
if (-not $OutputDir) {
	$OutputDir = Resolve-OutputDir
}
$OutputDir = (Resolve-Path $OutputDir).Path
$manifestPath = Join-Path $OutputDir "AppxManifest.xml"
$rid = if ($OutputDir -match '\\(win-(?:x64|x86|arm64))$') { $Matches[1] } else { 'win-x64' }
$winapp = Resolve-Winapp $rid

Write-Host "winapp:     $winapp"
Write-Host "Output dir: $OutputDir"

# winapp refuses to replace a package installed from an MSIX (non-development mode).
Get-AppxPackage -Name 'SamplesApp' -ErrorAction SilentlyContinue |
	Where-Object { -not $_.IsDevelopmentMode } |
	ForEach-Object {
		Write-Host "Removing conflicting MSIX-installed package: $($_.PackageFullName)"
		Remove-AppxPackage -Package $_.PackageFullName
	}

Set-DotnetRootForApp $OutputDir

# A stale results file left behind would later pass for this run's output.
foreach ($stale in @($ResultsFile, "$ResultsFile.canary")) {
	if (Test-Path $stale) {
		try {
			Remove-Item $stale -Force
		}
		catch {
			throw "Cannot remove the previous $stale (is another test run still writing it?): $_"
		}
	}
}

$winappArgs = @('run', (Format-PathArgument $OutputDir), '--manifest', (Format-PathArgument $manifestPath), '--with-alias')
if (-not $KeepRegistered) {
	$winappArgs += '--unregister-on-exit'
}
if ($DebugOutput) {
	$winappArgs += '--debug-output'
}
$winappArgs += '--'
$winappArgs += "--runtime-tests=$(Format-PathArgument $ResultsFile)"
if ($Filter) {
	$winappArgs += "--runtime-test-filter=$Filter"
}

Write-Host "Launching: winapp $($winappArgs -join ' ')"
$sw = [Diagnostics.Stopwatch]::StartNew()
$launchedAt = Get-Date
$process = Start-Process -FilePath $winapp -ArgumentList $winappArgs -PassThru -NoNewWindow

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
	if ($KeepRegistered) {
		# winapp stays up too: with -DebugOutput it is the app's debugger, and stopping it would kill the app.
		throw "Test run timed out after $TimeoutSeconds seconds. The app (winapp PID $($process.Id)) is left running and registered for inspection with winapp ui; stop it and run cleanup.ps1 when done."
	}
	Write-Host "Timeout after $TimeoutSeconds seconds — stopping the app."
	Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
	# Only this run's instance: it runs from this output folder and started after the launch.
	Get-Process -Name 'SamplesApp.Windows' -ErrorAction SilentlyContinue |
		Where-Object { $_.Path -and $_.Path.StartsWith("$OutputDir\", [StringComparison]::OrdinalIgnoreCase) -and $_.StartTime -ge $launchedAt.AddSeconds(-2) } |
		Stop-Process -Force
	& $winapp unregister --manifest $manifestPath --force | Out-Null
	throw "Test run timed out after $TimeoutSeconds seconds."
}

Write-Host ""
Write-Host "winapp exited with code $($process.ExitCode) after $($sw.Elapsed)."
if ($process.ExitCode -ne 0) {
	throw "winapp exited with code $($process.ExitCode) — registration, launch or the app itself failed. Any results file at $ResultsFile may be incomplete; re-run with -DebugOutput."
}

function Test-WrittenByThisRun([string]$path) {
	return (Test-Path $path) -and (Get-Item $path).LastWriteTime -ge $launchedAt.AddSeconds(-2)
}

if (-not (Test-WrittenByThisRun $ResultsFile)) {
	if (Test-WrittenByThisRun "$ResultsFile.canary") {
		throw "The app started (canary written) but produced no results file — it likely crashed mid-run. Re-run with -DebugOutput for a stack trace."
	}
	throw "No results file written by this run at $ResultsFile — the app never reached the runtime-test entry point."
}

Write-Host "Results: $ResultsFile"
