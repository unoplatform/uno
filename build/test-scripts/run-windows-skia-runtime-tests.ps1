Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}


$UNO_TESTS_FAILED_LIST="$env:BUILD_SOURCESDIRECTORY\build\uitests-failure-results\failed-tests-windows-runtimetests-windows-$env:UITEST_RUNTIME_TEST_GROUP.txt"
$TEST_RESULTS_FILE="$env:build_sourcesdirectory\build\skia-windows-runtime-tests-results.xml"

# Create the failed-tests directory up front so a crashed run still has somewhere to write,
# and PublishBuildArtifacts@1 does not retry a missing PathtoPublish.
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $UNO_TESTS_FAILED_LIST) | Out-Null

# A run that starts from a failed-tests list is a stage retry already, which does not re-run inline.
$isFirstPass = -not (Test-Path $UNO_TESTS_FAILED_LIST)

# convert the content of the file UNO_TESTS_FAILED_LIST to base64 and set it to UITEST_RUNTIME_TESTS_FILTER, if the file exists
if (Test-Path $UNO_TESTS_FAILED_LIST) {
    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content $UNO_TESTS_FAILED_LIST)))
    $env:UITEST_RUNTIME_TESTS_FILTER="$base64"
}

cd $env:SamplesAppArtifactPath
dotnet SamplesApp.dll --runtime-tests=$TEST_RESULTS_FILE

## Export the failed tests list for reuse in a pipeline retry
pushd $env:BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

Assert-ExitCodeIsZero

# Re-run the failed tests once, in a fresh app process: see runtime-tests-rerun.sh
if ($isFirstPass) {
    $RERUN_FILTER_FILE="$env:BUILD_SOURCESDIRECTORY\build\skia-windows-runtime-tests-rerun-filter.txt"
    $RERUN_RESULTS_FILE="$env:BUILD_SOURCESDIRECTORY\build\skia-windows-runtime-tests-rerun.xml"

    dotnet run rerun-filter $TEST_RESULTS_FILE 30 $RERUN_FILTER_FILE
    Assert-ExitCodeIsZero

    if (Test-Path $RERUN_FILTER_FILE) {
        $env:UITEST_RUNTIME_TESTS_FILTER = Get-Content $RERUN_FILTER_FILE -Raw

        $app = Start-Process dotnet -ArgumentList "SamplesApp.dll", "--runtime-tests=$RERUN_RESULTS_FILE" -WorkingDirectory $env:SamplesAppArtifactPath -NoNewWindow -PassThru
        if (-not $app.WaitForExit(600 * 1000)) {
            echo "##vso[task.logissue type=warning]The failed tests re-run did not finish within 600s."
            $app.Kill($true)
        }

        if (Test-Path $RERUN_RESULTS_FILE) {
            dotnet run merge-rerun $TEST_RESULTS_FILE $RERUN_RESULTS_FILE $TEST_RESULTS_FILE
            Assert-ExitCodeIsZero
        }
        else {
            echo "##vso[task.logissue type=warning]The failed tests re-run produced no results; keeping the first run's failures."
        }
    }
}

dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

popd
