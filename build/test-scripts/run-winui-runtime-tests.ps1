$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

function Write-CrashDetails($since)
{
    # The exit code alone doesn't say what crashed, and the lane collects no dumps.
    $events = Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = 'Application Error'; Id = 1000; StartTime = $since } -ErrorAction SilentlyContinue
    if ($events) {
        $events | ForEach-Object { Write-Host $_.Message }
    }
    else {
        Write-Host "No Application Error event was logged for this exit."
    }
}

$UNO_TESTS_FAILED_LIST="$env:BUILD_SOURCESDIRECTORY\build\uitests-failure-results\failed-tests-winui-runtimetests.txt"
$TEST_RESULTS_FILE="$env:BUILD_SOURCESDIRECTORY\build\winui-runtime-tests-results.xml"

# convert the content of the file UNO_TESTS_FAILED_LIST to base64 and set it to UITEST_RUNTIME_TESTS_FILTER, if the file exists
if (Test-Path $UNO_TESTS_FAILED_LIST) {
    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content -Raw $UNO_TESTS_FAILED_LIST)))
    $env:UITEST_RUNTIME_TESTS_FILTER="$base64"
}

# Build command-line arguments for runtime tests
$runtimeTestArgs = @("--runtime-tests=`"$TEST_RESULTS_FILE`"")

if ($env:UITEST_RUNTIME_TEST_GROUP) {
    $runtimeTestArgs += "--runtime-tests-group=$env:UITEST_RUNTIME_TEST_GROUP"
}

if ($env:UITEST_RUNTIME_TEST_GROUP_COUNT) {
    $runtimeTestArgs += "--runtime-tests-group-count=$env:UITEST_RUNTIME_TEST_GROUP_COUNT"
}

if ($env:UITEST_RUNTIME_TESTS_FILTER) {
    $runtimeTestArgs += "--runtime-test-filter=$env:UITEST_RUNTIME_TESTS_FILTER"
}

Write-Host "Runtime test arguments: $($runtimeTestArgs -join ' ')"

# Ensure the failed tests directory exists before running tests
$failedTestsDir = Split-Path -Parent $UNO_TESTS_FAILED_LIST
New-Item -ItemType Directory -Force -Path $failedTestsDir | Out-Null

# Use the app execution alias registered by the MSIX package
$exeAlias = "unosamplesapp.exe"

Write-Host "Using app execution alias: $exeAlias"

# A crash in native WinUI loses every result, and has landed on different tests, so it gets one more run.
# A crash that repeats, or a run that hangs, still fails.
$maxAttempts = 2

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    # Launch the app with runtime test arguments
    Write-Host "Launching app with runtime tests (attempt $attempt of $maxAttempts)..."

    $attemptStart = Get-Date
    $process = Start-Process -FilePath $exeAlias -ArgumentList $runtimeTestArgs -PassThru -NoNewWindow

    Write-Host "App launched with PID: $($process.Id)"
    Write-Host "Waiting for test results..."

    # Wait for the test results file with timeout
    $timeout = 600 # 10 minutes
    $elapsed = 0
    $checkInterval = 5

    while ($elapsed -lt $timeout) {
        # Check if process has exited
        if ($process.HasExited) {
            Write-Host "App process has exited with code: $($process.ExitCode)"
            break
        }

        if (Test-Path $TEST_RESULTS_FILE) {
            Write-Host "Test results file found!"
            # Wait a bit more for the app to finish writing and exit cleanly
            Start-Sleep -Seconds 5
            break
        }

        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
        Write-Host "Waiting for test results... ($elapsed seconds elapsed)"
    }

    if (Test-Path $TEST_RESULTS_FILE) {
        break
    }

    Write-Host "Test results file was not created within the timeout period"

    if ($process.HasExited -and $process.ExitCode -ne 0) {
        Write-CrashDetails $attemptStart

        if ($attempt -lt $maxAttempts) {
            Write-Host "##vso[task.logissue type=warning]The WinUI runtime tests app crashed with exit code $($process.ExitCode). Running the tests again."
            continue
        }
    }

    # Try to get the app process logs if still running
    if (-not $process.HasExited) {
        Write-Host "Force stopping the app..."
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }

    throw "Test results file was not created"
}

# Ensure the process has exited
if (-not $process.HasExited) {
    Write-Host "Waiting for app to exit gracefully..."
    $process.WaitForExit(30000) # Wait up to 30 seconds

    if (-not $process.HasExited) {
        Write-Host "Force stopping the app..."
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Test execution completed. Processing results..."

## Export the failed tests list for reuse in a pipeline retry
Push-Location "$env:BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"

Write-Host "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

Assert-ExitCodeIsZero

dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

Assert-ExitCodeIsZero

Pop-Location
