# run-tests-msix.ps1 — Launch the MSIX-installed app through its execution alias and wait
# for the runtime test results.
#
# Only for the MSIX fallback path (see SKILL.md). The default flow uses run-tests.ps1,
# which needs no packaging, signing or install.
#
# Usage:
#   pwsh -File run-tests-msix.ps1 -ResultsFile <path> [-Filter <base64-encoded-filter>]

param(
	[Parameter(Mandatory = $true)]
	[string]$ResultsFile,

	[Parameter(Mandatory = $false)]
	[string]$Filter = ""
)

$ErrorActionPreference = 'Stop'

$ResultsFile = [System.IO.Path]::GetFullPath($ResultsFile)

# The app is framework-dependent: an alias launch inherits this process's environment, so a
# side-by-side .NET runtime (net11 previews) has to be pointed at explicitly.
if (-not $env:DOTNET_ROOT -and (Test-Path "$env:LOCALAPPDATA\Microsoft\dotnet\shared\Microsoft.NETCore.App")) {
	$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
	Write-Host "DOTNET_ROOT set to $env:DOTNET_ROOT"
}

if (Test-Path $ResultsFile) {
	Remove-Item $ResultsFile -Force
}

$runtimeTestArgs = @("--runtime-tests=`"$ResultsFile`"")
if ($Filter) {
	$runtimeTestArgs += "--runtime-test-filter=$Filter"
}

Write-Host "Launching: unosamplesapp.exe $($runtimeTestArgs -join ' ')"
$process = Start-Process -FilePath "unosamplesapp.exe" -ArgumentList $runtimeTestArgs -PassThru -NoNewWindow

Write-Host "App launched with PID: $($process.Id)"
Write-Host "Waiting for test results..."

$timeout = 600
$elapsed = 0
$checkInterval = 5

while ($elapsed -lt $timeout) {
	# Check results file first (may appear just as process exits)
	if (Test-Path $ResultsFile) {
		Write-Host "Results file found after $elapsed seconds."
		Start-Sleep -Seconds 5
		break
	}
	if ($process.HasExited) {
		Write-Host "App exited with code: $($process.ExitCode)"
		$remaining = $timeout - $elapsed
		$grace = [Math]::Min(10, $remaining)
		Write-Host "Waiting up to $grace seconds for results file after process exit..."
		$graceElapsed = 0
		while (($graceElapsed -lt $grace) -and -not (Test-Path $ResultsFile)) {
			Start-Sleep -Seconds 1
			$graceElapsed++
			$elapsed++
		}
		if (Test-Path $ResultsFile) {
			Write-Host "Results file found after process exit (elapsed: $elapsed seconds)."
			Start-Sleep -Seconds 5
		}
		break
	}
	Start-Sleep -Seconds $checkInterval
	$elapsed += $checkInterval
	if ($elapsed % 30 -eq 0) {
		Write-Host "Still waiting... ($elapsed seconds elapsed)"
	}
}

if (-not (Test-Path $ResultsFile)) {
	if (-not $process.HasExited) {
		Write-Host "Timeout reached. Force-stopping app..."
		Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
	}
	throw "Test results file was not created within $timeout seconds."
}

if (-not $process.HasExited) {
	Write-Host "Waiting for app to exit gracefully..."
	$process.WaitForExit(30000)
	if (-not $process.HasExited) {
		Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
	}
}

Write-Host ""
Write-Host "Test execution completed."
Write-Host "Results: $ResultsFile"
