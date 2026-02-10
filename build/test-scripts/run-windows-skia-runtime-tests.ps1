Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

function Write-HeartbeatXml([string]$OutputPath, [string]$Message)
{
	$singleLine = ($Message -replace "[\r\n]+", " ").Trim()
	$escaped = [System.Security.SecurityElement]::Escape($singleLine)
	$xml = @"
<?xml version="1.0" encoding="utf-8"?>
<test-run id="0" name="RuntimeHeartbeat" testcasecount="1" result="Skipped" total="1" passed="0" failed="0" skipped="1" inconclusive="0" duration="0">
    <test-suite type="Assembly" name="RuntimeHeartbeat" result="Skipped" total="1" passed="0" failed="0" skipped="1" inconclusive="0" duration="0">
        <test-case id="0-0" name="LastHeartbeat" result="Skipped" duration="0">
            <reason>
                <message>$escaped</message>
            </reason>
        </test-case>
    </test-suite>
</test-run>
"@
	$xml | Set-Content -Path $OutputPath -Encoding UTF8
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
    $heartbeatMessage = (Get-Content $RUNTIME_CURRENT_TEST_FILE -Raw -Encoding Unicode).Trim()
    Write-Host "Last runtime test heartbeat: $heartbeatMessage"
} else {
    $heartbeatMessage = "No runtime test heartbeat file found."
    Write-Host "No runtime test heartbeat file found."
}

$heartbeatXmlPath = "$env:build_sourcesdirectory\build\runtime-heartbeat-windows-$env:UITEST_RUNTIME_TEST_GROUP.xml"
Write-HeartbeatXml -OutputPath $heartbeatXmlPath -Message $heartbeatMessage

## Export the failed tests list for reuse in a pipeline retry
pushd $env:BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST}) -Force

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

Assert-ExitCodeIsZero

dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

popd
