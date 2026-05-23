Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

function Invoke-NUnitTool([string[]]$Arguments)
{
    Push-Location "$env:BUILD_SOURCESDIRECTORY\src\Uno.NUnitTransformTool"
    try
    {
        dotnet run @Arguments
        Assert-ExitCodeIsZero
    }
    finally
    {
        Pop-Location
    }
}

function Invoke-NUnitToolCaptureOutput([string[]]$Arguments)
{
    Push-Location "$env:BUILD_SOURCESDIRECTORY\src\Uno.NUnitTransformTool"
    try
    {
        # dotnet run may emit build noise before the actual output; take only the last line.
        $result = (dotnet run @Arguments | Select-Object -Last 1)
        Assert-ExitCodeIsZero
        return $result
    }
    finally
    {
        Pop-Location
    }
}

# Set by Invoke-SkiaTests when the heartbeat watchdog fires.
$script:HungTestName = $null
$script:HungHbAge = 0

# Runs the Skia app with a heartbeat watchdog. Kills the process if no ##UNO-RT-HEARTBEAT##
# marker appears in stdout for more than $HeartbeatTimeoutS seconds.
# Returns $true when the result file was written, $false when the watchdog killed the app.
function Invoke-SkiaTests([string]$ResultsFile)
{
    $heartbeatLog = [System.IO.Path]::GetTempFileName()
    $heartbeatTimeoutS = [int]($env:HEARTBEAT_TIMEOUT_S ?? "300")
    $artifactPath = $env:SamplesAppArtifactPath

    # Run the app in a background job so we can monitor its stdout for heartbeat markers.
    $job = Start-Job -ScriptBlock {
        param($path, $dir, $hbLog)
        Push-Location $dir
        try
        {
            dotnet SamplesApp.Skia.Generic.dll "--runtime-tests=$path" 2>&1 | ForEach-Object {
                Write-Output $_
                if ($_ -match '##UNO-RT-HEARTBEAT##')
                {
                    [System.IO.File]::AppendAllText($hbLog, "$_`n")
                }
            }
        }
        finally
        {
            Pop-Location
        }
    } -ArgumentList $ResultsFile, $artifactPath, $heartbeatLog

    $watchdogFired = $false
    try
    {
        while ($job.State -eq 'Running')
        {
            Start-Sleep -Seconds 10
            Receive-Job $job  # flush output to console

            if ((Test-Path $heartbeatLog) -and (Get-Item $heartbeatLog).Length -gt 0)
            {
                $hbAge = ((Get-Date) - (Get-Item $heartbeatLog).LastWriteTime).TotalSeconds
                if ($hbAge -gt $heartbeatTimeoutS)
                {
                    Write-Host "##[error]No heartbeat for $([int]$hbAge)s — test appears hung. Last heartbeats:"
                    $lastLines = Get-Content $heartbeatLog -Tail 3
                    $lastLines | Write-Host

                    # Extract the hung test name from the last STARTING line.
                    $lastStarting = $lastLines | Where-Object { $_ -match 'STARTING' } | Select-Object -Last 1
                    if ($lastStarting -match 'STARTING\s+(.+)$')
                    {
                        $script:HungTestName = $Matches[1].Trim()
                        $script:HungHbAge = [int]$hbAge
                        Write-Host "##[error]Hung test identified: $($script:HungTestName) (no heartbeat for $([int]$hbAge)s)"
                    }

                    Stop-Job $job
                    $watchdogFired = $true
                    break
                }
            }
        }
        Receive-Job $job  # flush remaining output
    }
    finally
    {
        Remove-Job $job -Force -ErrorAction SilentlyContinue
        Remove-Item $heartbeatLog -Force -ErrorAction SilentlyContinue
    }

    return -not $watchdogFired
}

# Maximum failures to trigger an in-job retry; overridable via env var.
$FlakeRetryMaxFailures = [int]($env:FLAKE_RETRY_MAX_FAILURES ?? "20")

$UNO_TESTS_FAILED_LIST="$env:BUILD_SOURCESDIRECTORY\build\uitests-failure-results\failed-tests-windows-runtimetests-windows-$env:UITEST_RUNTIME_TEST_GROUP.txt"
$TEST_RESULTS_FILE="$env:build_sourcesdirectory\build\skia-windows-runtime-tests-results.xml"
$TEST_RESULTS_RERUN_FILE="$env:build_sourcesdirectory\build\skia-windows-runtime-tests-results-rerun.xml"

# convert the content of the file UNO_TESTS_FAILED_LIST to base64 and set it to UITEST_RUNTIME_TESTS_FILTER, if the file exists
if (Test-Path $UNO_TESTS_FAILED_LIST) {
    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content $UNO_TESTS_FAILED_LIST)))
    $env:UITEST_RUNTIME_TESTS_FILTER="$base64"
}

Invoke-SkiaTests $TEST_RESULTS_FILE

## Export the failed tests list for reuse in a pipeline retry
New-Item -ItemType Directory -Force -Path (Split-Path $UNO_TESTS_FAILED_LIST)

# When the watchdog fires the results file is never written; synthesise a failure record.
if (-not (Test-Path $TEST_RESULTS_FILE) -and $script:HungTestName)
{
    $hungSimple = $script:HungTestName -replace '\(.*', ''
    Write-Host "##[warning]Creating synthetic result for hung test '$hungSimple' ($($script:HungHbAge)s without heartbeat)."
    Invoke-NUnitTool "create-hung-result", $hungSimple, $TEST_RESULTS_FILE, "$($script:HungHbAge)"
}

Write-Host "Running NUnitTransformTool"

## Fail the build when no test results could be read
Invoke-NUnitTool "fail-empty", $TEST_RESULTS_FILE

$FAILED_COUNT = [int](Invoke-NUnitToolCaptureOutput "count-failed", $TEST_RESULTS_FILE)
Invoke-NUnitTool "list-failed", $TEST_RESULTS_FILE, $UNO_TESTS_FAILED_LIST

if ($script:HungTestName)
{
    # Watchdog fired: rerun the full shard excluding the hung test so we collect results
    # for all tests that never ran.  The hung test stays Failed in the merged output.
    $hungSimple = $script:HungTestName -replace '\(.*', ''
    Write-Host "##[warning]Watchdog retry: rerunning shard excluding '$hungSimple'..."
    $excludeFilter = "~$hungSimple"
    $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($excludeFilter))

    Invoke-SkiaTests $TEST_RESULTS_RERUN_FILE

    if (Test-Path $TEST_RESULTS_RERUN_FILE) {
        Invoke-NUnitTool "merge-results", $TEST_RESULTS_FILE, $TEST_RESULTS_RERUN_FILE, $TEST_RESULTS_FILE
        Invoke-NUnitTool "list-failed", $TEST_RESULTS_FILE, $UNO_TESTS_FAILED_LIST
    } else {
        Write-Host "##[warning]Watchdog retry did not produce a results file; original results kept."
    }
}
elseif ($FAILED_COUNT -gt 0 -and $FAILED_COUNT -le $FlakeRetryMaxFailures)
{
    # In-job retry: if a small number of tests failed, rerun only those to catch flakes
    Write-Host "##[warning]$FAILED_COUNT test(s) failed — retrying in-job to filter flakes..."

    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content $UNO_TESTS_FAILED_LIST)))
    $env:UITEST_RUNTIME_TESTS_FILTER = $base64

    Invoke-SkiaTests $TEST_RESULTS_RERUN_FILE

    if (Test-Path $TEST_RESULTS_RERUN_FILE) {
        Invoke-NUnitTool "merge-results", $TEST_RESULTS_FILE, $TEST_RESULTS_RERUN_FILE, $TEST_RESULTS_FILE
        Invoke-NUnitTool "list-failed", $TEST_RESULTS_FILE, $UNO_TESTS_FAILED_LIST
    } else {
        Write-Host "##[warning]Rerun did not produce a results file; original results kept."
    }
}
