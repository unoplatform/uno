<#
.SYNOPSIS
Determines which optional CI test scopes must run for a pull request based on the touched files.

.DESCRIPTION
This script inspects the diff between the PR head and its target branch to figure out which
optional test stages (template tests and the screenshot comparison) should execute. It sets
Azure DevOps variables (both standard and task output) for each scope so subsequent stages can
conditionally run.

When the script cannot determine scope (e.g., non-PR build or git fetch failure) it enables
every scope to stay safe.

.PARAMETER DefaultTargetBranch
Fallback branch used when System.PullRequest.TargetBranch is not available.
#>
param(
    [string]$DefaultTargetBranch = 'refs/heads/master'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scopeVariables = [ordered]@{
    TemplateTestsRequired = $false
    ScreenshotsRequired   = $false
}

# Heuristics:
#   TemplateTestsRequired - build infrastructure / project-shape changes that can affect the
#                           generated app templates.
#   ScreenshotsRequired   - any .cs change in Uno.UI and above (the UI layer: src/Uno.UI* + src/AddIns)
#                           that could affect rendered output.
$patterns = @{
    TemplateTestsRequired = [regex]'(?i)(?:^build/|\.csproj$|\.props$|\.targets$|^src/uno\.sdk/|^src/.*devserver.*|^src/.*remotecontrol.*)'
    ScreenshotsRequired   = [regex]'(?i)^src/(uno\.ui|addins).*\.cs$'
}

function Set-TestScopeVariable {
    param(
        [string]$Name,
        [bool]$Value
    )

    $stringValue = if ($Value) { 'true' } else { 'false' }
    Write-Host "$Name requires tests: $stringValue"
    Write-Host "##vso[task.setvariable variable=$Name]$stringValue"
    Write-Host "##vso[task.setvariable variable=$Name;isOutput=true]$stringValue"
}

function Publish-TestScopes {
    param([System.Collections.IDictionary]$Values)

    foreach ($key in @($Values.Keys)) {
        Set-TestScopeVariable -Name $key -Value $Values[$key]
    }
}

function Enable-AllScopes {
    param([System.Collections.IDictionary]$Values)

    foreach ($key in @($Values.Keys)) {
        $Values[$key] = $true
    }
}

if ($env:BUILD_REASON -ne 'PullRequest') {
    Enable-AllScopes -Values $scopeVariables
    Publish-TestScopes -Values $scopeVariables
    return
}

$targetRef = if ([string]::IsNullOrWhiteSpace($env:SYSTEM_PULLREQUEST_TARGETBRANCH)) {
    $DefaultTargetBranch
} else {
    $env:SYSTEM_PULLREQUEST_TARGETBRANCH
}

$prevErrorPref = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
git fetch origin $targetRef --depth=200 2>&1 | Out-Null
$fetchExitCode = $LASTEXITCODE
$ErrorActionPreference = $prevErrorPref

if ($fetchExitCode -ne 0) {
    Write-Warning ("Unable to fetch {0} (exit code {1}). Falling back to enabling all test scopes." -f $targetRef, $fetchExitCode)
    Enable-AllScopes -Values $scopeVariables
    Publish-TestScopes -Values $scopeVariables
    # Reset LASTEXITCODE so the Azure pipeline step does not fail due to git fetch's non-zero exit code.
    $global:LASTEXITCODE = 0
    return
}

$diffArgs = @('--name-only', 'FETCH_HEAD..HEAD')
$changedFiles = git diff @diffArgs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

if (-not $changedFiles) {
    Publish-TestScopes -Values $scopeVariables
    return
}

foreach ($file in $changedFiles) {
    $trimmed = $file.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        continue
    }

    $normalized = $trimmed -replace '\\', '/'

    foreach ($scope in $patterns.GetEnumerator()) {
        if (-not $scopeVariables[$scope.Key] -and $scope.Value.IsMatch($normalized)) {
            Write-Host "File '$normalized' matches pattern for $($scope.Key)."
            $scopeVariables[$scope.Key] = $true
        }
    }
}

Publish-TestScopes -Values $scopeVariables
