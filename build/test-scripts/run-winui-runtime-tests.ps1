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

# A native crash (e.g. 0xC0000005) leaves no results file and no managed stack. Keep a dump and the
# app output, published by the pipeline as `runtime-tests-winappsdk-crash` when the job fails.
$CRASH_DIR = "$env:BUILD_SOURCESDIRECTORY\build\winui-crash-dumps"
$APP_OUTPUT_FILE = "$CRASH_DIR\app-output.txt"
New-Item -ItemType Directory -Force -Path $CRASH_DIR | Out-Null

$localDumpsKey = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
try {
    New-Item -Path $localDumpsKey -Force | Out-Null
    New-ItemProperty -Path $localDumpsKey -Name DumpFolder -Value $CRASH_DIR -PropertyType ExpandString -Force | Out-Null
    New-ItemProperty -Path $localDumpsKey -Name DumpType -Value 2 -PropertyType DWord -Force | Out-Null # full dump
    New-ItemProperty -Path $localDumpsKey -Name DumpCount -Value 1 -PropertyType DWord -Force | Out-Null
    Write-Host "WER LocalDumps enabled, dumps go to $CRASH_DIR"
}
catch {
    Write-Host "Could not enable WER LocalDumps (crash dumps will not be collected): $_"
}

# Use the app execution alias registered by the MSIX package
$exeAlias = "unosamplesapp.exe"

Write-Host "Using app execution alias: $exeAlias"

# Launch the app with runtime test arguments
Write-Host "Launching app with runtime tests..."

# The output goes to a file (echoed in the poll loop below) so the last started test can be named on a crash.
$process = Start-Process -FilePath $exeAlias -ArgumentList $runtimeTestArgs -PassThru -NoNewWindow `
    -RedirectStandardOutput $APP_OUTPUT_FILE -RedirectStandardError "$CRASH_DIR\app-error.txt"
$outputOffset = 0

function Write-NewAppOutput()
{
    if (-not (Test-Path $APP_OUTPUT_FILE)) { return }

    $stream = [IO.File]::Open($APP_OUTPUT_FILE, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        $null = $stream.Seek($script:outputOffset, [IO.SeekOrigin]::Begin)
        $text = (New-Object IO.StreamReader($stream)).ReadToEnd()
        $script:outputOffset = $stream.Position
        if ($text) { Write-Host $text.TrimEnd() }
    }
    finally {
        $stream.Dispose()
    }
}

Write-Host "App launched with PID: $($process.Id)"
Write-Host "Waiting for test results..."

# Wait for the test results file with timeout
$timeout = 600 # 10 minutes
$elapsed = 0
$checkInterval = 5

while ($elapsed -lt $timeout) {
    Write-NewAppOutput

    # Check if process has exited
    if ($process.HasExited) {
        Write-Host "App process has exited with code: $($process.ExitCode)"
        Start-Sleep -Seconds 2 # let the output flush
        Write-NewAppOutput
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

    if ($process.HasExited) {
        Start-Sleep -Seconds 5 # WER may still be writing the dump
        $lastTest = $null
        if (Test-Path $APP_OUTPUT_FILE) {
            $lastTest = Select-String -Path $APP_OUTPUT_FILE -Pattern '^Running test (.+)$' | Select-Object -Last 1
        }

        $testName = if ($lastTest) { $lastTest.Matches[0].Groups[1].Value } else { "<no test had started>" }
        $exitCode = '0x{0:X8}' -f $process.ExitCode
        Write-Host "##vso[task.logissue type=error]The WinUI runtime tests app crashed with exit code $exitCode. Last test started: $testName"

        $dumps = Get-ChildItem -Path $CRASH_DIR -Filter *.dmp -ErrorAction SilentlyContinue
        if ($dumps) {
            # Full dumps are large and compress well: zip them so the artifact upload stays small.
            foreach ($dump in $dumps) {
                try {
                    $zip = "$($dump.FullName).zip"
                    Compress-Archive -Path $dump.FullName -DestinationPath $zip -CompressionLevel Optimal -Force
                    Write-Host "Zipped $($dump.Name): $([math]::Round($dump.Length / 1MB)) MB -> $([math]::Round((Get-Item $zip).Length / 1MB)) MB"
                    Remove-Item $dump.FullName -Force
                }
                catch {
                    Write-Host "Could not zip $($dump.Name), keeping it as is: $_"
                }
            }

            Write-Host "Crash dumps: $((Get-ChildItem -Path $CRASH_DIR -Filter *.dmp*).Name -join ', ') (artifact runtime-tests-winappsdk-crash)"
        }
        else {
            Write-Host "No crash dump was written to $CRASH_DIR."
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
