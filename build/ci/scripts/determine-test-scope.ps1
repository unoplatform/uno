<#
.SYNOPSIS
Determines which optional CI test scopes must run for a pull request based on the touched files.

.DESCRIPTION
This script inspects the diff between the PR head and its target branch to figure out which
optional test stages (template tests, the screenshot comparison, iOS and tvOS) should execute. It sets
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
    WebGpuTestsRequired   = $false
    UIKitTestsRequired    = $false
}

# Heuristics:
#   TemplateTestsRequired - build infrastructure / project-shape changes that can affect the
#                           generated app templates.
#   ScreenshotsRequired   - any .cs change in Uno.UI and above (the UI layer: src/Uno.UI* + src/AddIns)
#                           that could affect rendered output.
#   WebGpuTestsRequired   - the WebGPU backend, its native packaging and loaders, or the runtime-test
#                           infrastructure. Gates every WebGPU lane but Linux's, which always runs.
#   UIKitTestsRequired    - iOS/tvOS code, the Apple toolchain (Xcode image, SDK, workloads) or the
#                           iOS/tvOS CI plumbing. Gates the iOS and tvOS stages, which run on
#                           per-minute Mac agents; other changes get them on master. Matches
#                           PascalCase names (iOSResourceConverter) and the files that gate these
#                           lanes, so a change to the gate runs the lanes it gates. Shared files
#                           are caught by their changed lines instead (see $uikitContentPattern).
$patterns = @{
    TemplateTestsRequired = [regex]'(?i)(?:^build/|\.csproj$|\.props$|\.targets$|^src/uno\.sdk/|^src/sourcegenerators/uno\.ui\.tasks/|^src/.*devserver.*|^src/.*remotecontrol.*)'
    ScreenshotsRequired   = [regex]'(?i)^src/(uno\.ui|addins).*\.cs$'
    WebGpuTestsRequired   = [regex]'(?i)(?:webgpu|wgpu|^build/ci/tests/|^build/test-scripts/)'
    UIKitTestsRequired    = [regex]'(?i)(?:^|[/._-])(?:tvos|ios|uikit|appleuikit|apple)(?:[/._-]|$)|(?:^|[/._-])(?-i:iOS|tvOS|UIKit|Apple)(?-i:[A-Z])|^\.vsts-ci\.yml$|(?:^|/)_?global\.json$|^build/ci/templates/(?:dotnet-mobile-install-mac|ios-build-select-version|mac-rerun-limit)\.yml$|^build/ci/\.azure-devops-stages\.yml$|^build/ci/tests/\.azure-devops-tests-runtime-stages\.yml$|^build/ci/scripts/determine-test-scope\.ps1$'
}

# iOS/tvOS-only code in shared files: platform conditionals, OperatingSystem checks and iOS-only
# runtime tests. git's -G matches these in the PR's added or removed lines.
$uikitContentPattern = '__IOS__|__APPLE_UIKIT__|__TVOS__|IsIOS\(\)|IsTvOS\(\)|IsMacCatalyst\(\)|Skia(IOS|UIKit|TvOS)'

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

if (-not $scopeVariables['UIKitTestsRequired']) {
    $uikitFiles = @(git diff --name-only "-G$uikitContentPattern" FETCH_HEAD..HEAD | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Unable to search the diff for iOS/tvOS code. Enabling UIKitTestsRequired."
        $scopeVariables['UIKitTestsRequired'] = $true
        $global:LASTEXITCODE = 0
    } elseif ($uikitFiles.Count -gt 0) {
        Write-Host "Changed lines in '$($uikitFiles[0])' match iOS/tvOS code for UIKitTestsRequired."
        $scopeVariables['UIKitTestsRequired'] = $true
    }
}

Publish-TestScopes -Values $scopeVariables
