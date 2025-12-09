Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

Write-Host "Running WinUI Runtime Tests..."

# Create test results directory if it doesn't exist
$testResultsDir = "$env:Build_SourcesDirectory\artifacts\TestResults"
New-Item -ItemType Directory -Force -Path $testResultsDir | Out-Null

# Run the tests
dotnet test src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Windows.csproj `
    -c Release `
    -p:UNO_DISABLE_ANALYZERS_IN_SAMPLES=true `
    --no-build `
    --logger "trx;LogFileName=winui-runtime-tests.trx" `
    --results-directory $testResultsDir `
    -bl:$env:build_artifactstagingdirectory\winui-runtime-tests-run.binlog

Assert-ExitCodeIsZero

Write-Host "WinUI Runtime Tests completed successfully."
