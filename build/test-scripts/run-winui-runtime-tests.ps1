Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

$UNO_TESTS_FAILED_LIST="$env:BUILD_SOURCESDIRECTORY\build\uitests-failure-results\failed-tests-winui-runtimetests.txt"
$TEST_RESULTS_FILE="$env:BUILD_SOURCESDIRECTORY\build\winui-runtime-tests-results.xml"

# convert the content of the file UNO_TESTS_FAILED_LIST to base64 and set it to UITEST_RUNTIME_TESTS_FILTER, if the file exists
if (Test-Path $UNO_TESTS_FAILED_LIST) {
    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content $UNO_TESTS_FAILED_LIST)))
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

# Launch the app with runtime test arguments
Write-Host "Launching app with runtime tests..."

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

if (-not (Test-Path $TEST_RESULTS_FILE)) {
    Write-Host "Test results file was not created within the timeout period"
    
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

Pop-Location
