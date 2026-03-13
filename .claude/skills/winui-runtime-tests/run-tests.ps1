# run-tests.ps1 — Launch unosamplesapp.exe and wait for runtime test results
#
# Usage:
#   pwsh -File run-tests.ps1 -ResultsFile <path> [-Filter <base64-encoded-filter>]
#
# The filter is a base64-encoded, pipe-separated list of fully qualified test names.

param(
    [Parameter(Mandatory=$true)]
    [string]$ResultsFile,

    [Parameter(Mandatory=$false)]
    [string]$Filter = ""
)

$ErrorActionPreference = 'Stop'

# Resolve to absolute path
$ResultsFile = [System.IO.Path]::GetFullPath($ResultsFile)

# Remove old results
if (Test-Path $ResultsFile) {
    Remove-Item $ResultsFile -Force
}
# Build arguments
$runtimeTestArgs = @("--runtime-tests=`"$ResultsFile`"")
if ($Filter) {
    $runtimeTestArgs += "--runtime-test-filter=$Filter"
}

Write-Host "Launching: unosamplesapp.exe $($runtimeTestArgs -join ' ')"
$process = Start-Process -FilePath "unosamplesapp.exe" -ArgumentList $runtimeTestArgs -PassThru -NoNewWindow

Write-Host "App launched with PID: $($process.Id)"
Write-Host "Waiting for test results..."

# Wait for results with timeout (10 minutes)
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
        # Grace period: wait up to 10s for results file after exit
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

# Ensure process exits cleanly
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
