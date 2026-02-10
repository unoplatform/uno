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
$RUNTIME_CURRENT_TEST_FILE="$env:build_sourcesdirectory\build\runtime-current-test-windows-$env:UITEST_RUNTIME_TEST_GROUP.txt"

# CI uses this file to report the last started test when a shard hangs.
$env:UITEST_RUNTIME_CURRENT_TEST_FILE = $RUNTIME_CURRENT_TEST_FILE

# convert the content of the file UNO_TESTS_FAILED_LIST to base64 and set it to UITEST_RUNTIME_TESTS_FILTER, if the file exists
if (Test-Path $UNO_TESTS_FAILED_LIST) {
    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content $UNO_TESTS_FAILED_LIST)))
    $env:UITEST_RUNTIME_TESTS_FILTER="$base64"
}

cd $env:SamplesAppArtifactPath
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$TEST_RESULTS_FILE

if (Test-Path $RUNTIME_CURRENT_TEST_FILE) {
    Write-Host "Last runtime test heartbeat: $(Get-Content $RUNTIME_CURRENT_TEST_FILE)"
} else {
    Write-Host "No runtime test heartbeat file found."
}

## Export the failed tests list for reuse in a pipeline retry
pushd $env:BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST}) -Force

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

Assert-ExitCodeIsZero

dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

popd
