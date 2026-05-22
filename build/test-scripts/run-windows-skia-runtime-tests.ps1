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

# Runs the Skia app with a heartbeat watchdog. Kills the process if no ##UNO-RT-HEARTBEAT##
# marker appears in stdout for more than $HeartbeatTimeoutS seconds.
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
                    Get-Content $heartbeatLog -Tail 3 | Write-Host
                    Stop-Job $job
                    Remove-Item $heartbeatLog -Force -ErrorAction SilentlyContinue
                    exit 1
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

Write-Host "Running NUnitTransformTool"

## Fail the build when no test results could be read
Invoke-NUnitTool "fail-empty", $TEST_RESULTS_FILE

$FAILED_COUNT = [int](Invoke-NUnitToolCaptureOutput "count-failed", $TEST_RESULTS_FILE)
Invoke-NUnitTool "list-failed", $TEST_RESULTS_FILE, $UNO_TESTS_FAILED_LIST

# In-job retry: if a small number of tests failed, rerun only those to catch flakes
if ($FAILED_COUNT -gt 0 -and $FAILED_COUNT -le $FlakeRetryMaxFailures) {
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
