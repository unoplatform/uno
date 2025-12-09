Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

$UNO_TESTS_FAILED_LIST="$env:BUILD_SOURCESDIRECTORY\build\uitests-failure-results\failed-tests-winui-runtimetests-$env:UITEST_RUNTIME_TEST_GROUP.txt"
$TEST_RESULTS_FILE="$env:BUILD_SOURCESDIRECTORY\build\winui-runtime-tests-results.xml"

# convert the content of the file UNO_TESTS_FAILED_LIST to base64 and set it to UITEST_RUNTIME_TESTS_FILTER, if the file exists
if (Test-Path $UNO_TESTS_FAILED_LIST) {
    $base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Content $UNO_TESTS_FAILED_LIST)))
    $env:UITEST_RUNTIME_TESTS_FILTER="$base64"
}

# Build command-line arguments for runtime tests
$runtimeTestArgs = "--runtime-tests=$TEST_RESULTS_FILE"

if ($env:UITEST_RUNTIME_TEST_GROUP) {
    $runtimeTestArgs += " --runtime-tests-group=$env:UITEST_RUNTIME_TEST_GROUP"
}

if ($env:UITEST_RUNTIME_TEST_GROUP_COUNT) {
    $runtimeTestArgs += " --runtime-tests-group-count=$env:UITEST_RUNTIME_TEST_GROUP_COUNT"
}

if ($env:UITEST_RUNTIME_TESTS_FILTER) {
    $runtimeTestArgs += " --runtime-test-filter=$env:UITEST_RUNTIME_TESTS_FILTER"
}

Write-Host "Runtime test arguments: $runtimeTestArgs"

# Find the WinAppSDK package
$packagePath = Get-ChildItem -Path $env:SamplesAppArtifactPath -Filter "*.msix" -Recurse | Select-Object -First 1

if (-not $packagePath) {
    throw "Could not find MSIX package in $env:SamplesAppArtifactPath"
}

Write-Host "Found package: $($packagePath.FullName)"

# Install the package
Write-Host "Installing package..."
Add-AppxPackage -Path $packagePath.FullName -ForceApplicationShutdown

# Get the package full name
$packageName = "uno.platform.SamplesApp-dev"
$installedPackage = Get-AppxPackage -Name $packageName

if (-not $installedPackage) {
    throw "Package $packageName was not installed successfully"
}

Write-Host "Package installed: $($installedPackage.PackageFullName)"

# Get the app executable info
$packageFamilyName = $installedPackage.PackageFamilyName
$appId = "App"

Write-Host "Launching app with runtime tests..."

# Start the app with arguments using explorer.exe shell:AppsFolder protocol
# This is the reliable way to pass arguments to packaged WinUI apps
$appUserModelId = "${packageFamilyName}!${appId}"

# Create a VBS script to launch the app with arguments (workaround for passing args to MSIX apps)
$vbsScript = @"
Set objShell = CreateObject("WScript.Shell")
objShell.Run "shell:AppsFolder\$appUserModelId $runtimeTestArgs"
"@

$vbsPath = "$env:TEMP\launch-samplesapp.vbs"
$vbsScript | Out-File -FilePath $vbsPath -Encoding ASCII

# Launch via VBS script
cscript.exe //nologo $vbsPath

Write-Host "App launched, waiting for test results..."

# Wait for the test results file with timeout
$timeout = 600 # 10 minutes
$elapsed = 0
$checkInterval = 5

while ($elapsed -lt $timeout) {
    if (Test-Path $TEST_RESULTS_FILE) {
        Write-Host "Test results file found!"
        break
    }
    
    Start-Sleep -Seconds $checkInterval
    $elapsed += $checkInterval
    Write-Host "Waiting for test results... ($elapsed seconds elapsed)"
}

if (-not (Test-Path $TEST_RESULTS_FILE)) {
    throw "Test results file was not created within the timeout period"
}

# Give the app a moment to finish writing
Start-Sleep -Seconds 5

Write-Host "Test execution completed. Processing results..."

# Stop the app
Get-Process | Where-Object { $_.ProcessName -like "*SamplesApp*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Uninstall the package for cleanup
Write-Host "Cleaning up package..."
Remove-AppxPackage -Package $installedPackage.PackageFullName -ErrorAction SilentlyContinue

## Export the failed tests list for reuse in a pipeline retry
Push-Location "$env:BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"
$failedTestsDir = Split-Path -Parent $UNO_TESTS_FAILED_LIST
New-Item -ItemType Directory -Force -Path $failedTestsDir | Out-Null

Write-Host "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

Assert-ExitCodeIsZero

dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

Pop-Location
