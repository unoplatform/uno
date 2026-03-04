<#
.SYNOPSIS
    Repeatedly runs WinAppSDK runtime tests to diagnose intermittent crashes.

.DESCRIPTION
    Launches the SamplesApp MSIX multiple times, detects crashes (process exit
    without a results file), and analyzes the rolling test-tracker logs to
    identify which tests correlate with crashes.

.PARAMETER TotalRuns
    Number of times to run the test suite (default 20).

.PARAMETER Iterations
    Value for --runtime-tests-iterations per run (default 1).

.PARAMETER TimeoutSeconds
    Per-run timeout in seconds (default 600).

.PARAMETER TestFilter
    Optional base64-encoded filter string to narrow test scope.

.EXAMPLE
    .\run-winui-runtime-tests-repeat.ps1 -TotalRuns 20 -Iterations 1
    .\run-winui-runtime-tests-repeat.ps1 -TotalRuns 10 -Iterations 3 -TimeoutSeconds 900
#>

param(
    [int]$TotalRuns = 20,
    [int]$Iterations = 1,
    [int]$TimeoutSeconds = 600,
    [string]$TestFilter = ""
)

$ErrorActionPreference = 'Continue'

$trackingDir = Join-Path $env:TEMP "uno-test-tracking"
$resultsDir = Join-Path $env:TEMP "uno-repeat-results"
$exeAlias = "unosamplesapp.exe"

# Clean up previous tracking data
if (Test-Path $trackingDir) {
    Remove-Item -Recurse -Force $trackingDir
}
New-Item -ItemType Directory -Force -Path $trackingDir | Out-Null
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$crashRuns = @()
$successRuns = @()
$totalCrashes = 0
$totalSuccesses = 0

Write-Host "=========================================="
Write-Host " WinUI Runtime Tests - Repeat Runner"
Write-Host "=========================================="
Write-Host "Total runs:    $TotalRuns"
Write-Host "Iterations:    $Iterations"
Write-Host "Timeout:       ${TimeoutSeconds}s"
Write-Host "Tracking dir:  $trackingDir"
Write-Host "=========================================="
Write-Host ""

for ($run = 1; $run -le $TotalRuns; $run++) {
    $runResultFile = Join-Path $resultsDir "run-${run}-results.xml"

    # Remove previous result file
    if (Test-Path $runResultFile) {
        Remove-Item -Force $runResultFile
    }

    # Snapshot tracker files before this run
    $trackerFilesBefore = @(Get-ChildItem -Path $trackingDir -Filter "test-tracker-*.log" -ErrorAction SilentlyContinue | ForEach-Object { $_.Name })

    # Build arguments
    $runtimeTestArgs = @("--runtime-tests=`"$runResultFile`"")

    if ($Iterations -gt 1) {
        $runtimeTestArgs += "--runtime-tests-iterations=$Iterations"
    }

    if ($TestFilter) {
        $runtimeTestArgs += "--runtime-test-filter=$TestFilter"
    }

    Write-Host "--- Run $run / $TotalRuns ---"
    Write-Host "  Launching: $exeAlias $($runtimeTestArgs -join ' ')"

    $process = Start-Process -FilePath $exeAlias -ArgumentList $runtimeTestArgs -PassThru -NoNewWindow
    $startTime = Get-Date

    Write-Host "  PID: $($process.Id)"

    # Wait for completion
    $elapsed = 0
    $checkInterval = 5
    $crashed = $false

    while ($elapsed -lt $TimeoutSeconds) {
        if ($process.HasExited) {
            if (-not (Test-Path $runResultFile)) {
                Write-Host "  CRASH DETECTED - Process exited (code $($process.ExitCode)) without producing results"
                $crashed = $true
            }
            break
        }

        if (Test-Path $runResultFile) {
            Write-Host "  Results file found, waiting for process to exit..."
            Start-Sleep -Seconds 5
            break
        }

        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
    }

    # Handle timeout
    if (-not $process.HasExited) {
        if (-not (Test-Path $runResultFile)) {
            Write-Host "  TIMEOUT - No results after ${TimeoutSeconds}s, killing process"
            $crashed = $true
        }
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }

    # Identify new tracker files from this run
    $trackerFilesAfter = @(Get-ChildItem -Path $trackingDir -Filter "test-tracker-*.log" -ErrorAction SilentlyContinue | ForEach-Object { $_.Name })
    $newTrackerFiles = @($trackerFilesAfter | Where-Object { $_ -notin $trackerFilesBefore })

    $duration = ((Get-Date) - $startTime).TotalSeconds

    if ($crashed) {
        $totalCrashes++
        $crashRuns += @{
            Run = $run
            TrackerFiles = $newTrackerFiles
            Duration = [math]::Round($duration, 1)
            ExitCode = $process.ExitCode
        }
        Write-Host "  Result: CRASHED (${duration}s, exit code $($process.ExitCode))"
    } else {
        $totalSuccesses++
        $successRuns += @{
            Run = $run
            TrackerFiles = $newTrackerFiles
            Duration = [math]::Round($duration, 1)
        }
        Write-Host "  Result: OK (${duration}s)"
    }

    Write-Host "  Tracker files: $($newTrackerFiles -join ', ')"
    Write-Host ""
}

# ==========================================
# CRASH ANALYSIS
# ==========================================

Write-Host ""
Write-Host "=========================================="
Write-Host " EXECUTION SUMMARY"
Write-Host "=========================================="
Write-Host "Total runs:     $TotalRuns"
Write-Host "Successes:      $totalSuccesses"
Write-Host "Crashes:        $totalCrashes"
Write-Host "Crash rate:     $([math]::Round($totalCrashes / $TotalRuns * 100, 1))%"
Write-Host ""

if ($totalCrashes -eq 0) {
    Write-Host "No crashes detected. Consider increasing -Iterations or -TotalRuns to amplify."
    exit 0
}

Write-Host "=========================================="
Write-Host " CRASH CORRELATION SUMMARY"
Write-Host "=========================================="
Write-Host ""

# Collect crash data from tracker files
$lastStartedBeforeCrash = @{}
$lastCompletedBeforeCrash = @{}
$startedButNotCompleted = @{}
$lastEntriesBeforeCrash = @()

foreach ($crashRun in $crashRuns) {
    foreach ($trackerFile in $crashRun.TrackerFiles) {
        $filePath = Join-Path $trackingDir $trackerFile

        if (-not (Test-Path $filePath)) {
            continue
        }

        $lines = Get-Content $filePath
        $hasTrackerEnd = $lines | Where-Object { $_ -match "^TRACKER_END\|" }

        if ($hasTrackerEnd) {
            # Normal exit - not actually a crash tracker
            continue
        }

        # Find the last STARTING entry (test that was running when crash happened)
        $lastStarting = $null
        $lastCompleted = $null
        $startedTests = @{}
        $resolvedTests = @{}

        foreach ($line in $lines) {
            $parts = $line -split '\|', 3
            if ($parts.Length -lt 3) { continue }

            $status = $parts[0]
            $detail = $parts[2]

            switch ($status) {
                "STARTING" {
                    $lastStarting = $detail
                    $startedTests[$detail] = $true
                }
                "COMPLETED" {
                    $lastCompleted = $detail
                    $resolvedTests[$detail] = $true
                }
                "FAILED" {
                    $resolvedTests[($detail -split '\|')[0]] = $true
                }
                "INCONCLUSIVE" {
                    $resolvedTests[$detail] = $true
                }
                "SKIPPED" {
                    $resolvedTests[$detail] = $true
                }
            }
        }

        # Test that STARTED but never resolved (crashed mid-test)
        foreach ($test in $startedTests.Keys) {
            if (-not $resolvedTests.ContainsKey($test)) {
                if (-not $startedButNotCompleted.ContainsKey($test)) {
                    $startedButNotCompleted[$test] = 0
                }
                $startedButNotCompleted[$test]++
            }
        }

        # Last STARTED test before crash
        if ($lastStarting) {
            if (-not $lastStartedBeforeCrash.ContainsKey($lastStarting)) {
                $lastStartedBeforeCrash[$lastStarting] = 0
            }
            $lastStartedBeforeCrash[$lastStarting]++
        }

        # Last COMPLETED test before crash (delayed crash scenario)
        if ($lastCompleted) {
            if (-not $lastCompletedBeforeCrash.ContainsKey($lastCompleted)) {
                $lastCompletedBeforeCrash[$lastCompleted] = 0
            }
            $lastCompletedBeforeCrash[$lastCompleted]++
        }

        # Last N entries before crash
        $tailCount = [math]::Min(10, $lines.Length)
        $lastEntriesBeforeCrash += @{
            Run = $crashRun.Run
            File = $trackerFile
            Entries = $lines[($lines.Length - $tailCount)..($lines.Length - 1)]
        }
    }
}

# Report: Tests that STARTED but never COMPLETED
Write-Host "--- Tests that STARTED but never COMPLETED (crashed mid-test) ---"
if ($startedButNotCompleted.Count -eq 0) {
    Write-Host "  (none)"
} else {
    $startedButNotCompleted.GetEnumerator() | Sort-Object -Property Value -Descending | ForEach-Object {
        Write-Host ("  [{0}x] {1}" -f $_.Value, $_.Key)
    }
}
Write-Host ""

# Report: Last STARTED test before crash
Write-Host "--- Last STARTED test before crash ---"
if ($lastStartedBeforeCrash.Count -eq 0) {
    Write-Host "  (none)"
} else {
    $lastStartedBeforeCrash.GetEnumerator() | Sort-Object -Property Value -Descending | ForEach-Object {
        Write-Host ("  [{0}x] {1}" -f $_.Value, $_.Key)
    }
}
Write-Host ""

# Report: Last COMPLETED test before crash (delayed crash)
Write-Host "--- Last COMPLETED test before crash (delayed crash scenario) ---"
if ($lastCompletedBeforeCrash.Count -eq 0) {
    Write-Host "  (none)"
} else {
    $lastCompletedBeforeCrash.GetEnumerator() | Sort-Object -Property Value -Descending | ForEach-Object {
        Write-Host ("  [{0}x] {1}" -f $_.Value, $_.Key)
    }
}
Write-Host ""

# Report: Last 10 entries before each crash
Write-Host "--- Last entries before each crash ---"
foreach ($entry in $lastEntriesBeforeCrash) {
    Write-Host "  Run $($entry.Run) ($($entry.File)):"
    foreach ($line in $entry.Entries) {
        Write-Host "    $line"
    }
    Write-Host ""
}

Write-Host "=========================================="
Write-Host " Tracker files are in: $trackingDir"
Write-Host "=========================================="
