<#
.SYNOPSIS
Determines which optional CI test scopes must run for a pull request based on the touched files
and, for iOS/tvOS, the changed lines.

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
#                           are caught by their changed lines instead (see $uikitContentPattern),
#                           PRs into any branch other than master always enable it, and so does
#                           the ci/run-ios label ($uikitLabel).
$patterns = @{
    TemplateTestsRequired = [regex]'(?i)(?:^build/|\.csproj$|\.props$|\.targets$|^src/uno\.sdk/|^src/sourcegenerators/uno\.ui\.tasks/|^src/.*devserver.*|^src/.*remotecontrol.*)'
    ScreenshotsRequired   = [regex]'(?i)^src/(uno\.ui|addins).*\.cs$'
    WebGpuTestsRequired   = [regex]'(?i)(?:webgpu|wgpu|^build/ci/tests/|^build/test-scripts/)'
    UIKitTestsRequired    = [regex]'(?i)(?:^|[/._-])(?:tvos|ios|uikit|appleuikit|apple)(?:[/._-]|$)|(?:^|[/._-])(?-i:iOS|tvOS|UIKit|Apple)(?-i:[A-Z])|^\.vsts-ci\.yml$|(?:^|/)_?global\.json$|^build/ci/templates/(?:dotnet-mobile-install-mac|ios-build-select-version|mac-rerun-limit)\.yml$|^build/ci/\.azure-devops-stages\.yml$|^build/ci/tests/\.azure-devops-tests-runtime-stages\.yml$|^build/ci/scripts/determine-test-scope\.ps1$'
}

# iOS/tvOS-only code in shared files: C# conditionals and platform checks, iOS-only runtime-test
# platforms, MSBuild conditions and target frameworks, and XAML ios:/tvos: attributes.
# git's -G (POSIX extended regex) matches these in the PR's added or removed lines.
$uikitContentPattern = '__IOS__|__APPLE_UIKIT__|__TVOS__|Is(IOS|TvOS)(\(|VersionAtLeast)|IsUIKit\(|IsMobile\(|Skia(IOS|UIKit|TvOS|Mobile)|\$\(Is(IOS|TvOS|AppleUIKit)\)|-(ios|tvos)[;<"'']|uno\.ui/(ios|tvos)|[ <](ios|tvos):'

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

# Release and servicing PRs stabilize a branch, so they keep the iOS stage.
if ($targetRef -notmatch '^(refs/heads/)?master$') {
    Write-Host "Target branch '$targetRef' is not master: enabling UIKitTestsRequired."
    $scopeVariables['UIKitTestsRequired'] = $true
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

# --no-renames lists both sides of a move, so a file moved out of an iOS folder still counts.
$ErrorActionPreference = 'Continue'
$changedFiles = git diff --name-only --no-renames FETCH_HEAD..HEAD | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
$diffExitCode = $LASTEXITCODE
$ErrorActionPreference = $prevErrorPref

if ($diffExitCode -ne 0) {
    Write-Host ("##vso[task.logissue type=warning]Unable to list the changed files (git exit code {0}). Enabling all test scopes." -f $diffExitCode)
    Enable-AllScopes -Values $scopeVariables
    Publish-TestScopes -Values $scopeVariables
    $global:LASTEXITCODE = 0
    return
}

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
    $ErrorActionPreference = 'Continue'
    $uikitFiles = @(git diff --name-only --no-renames "-G$uikitContentPattern" FETCH_HEAD..HEAD | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $searchExitCode = $LASTEXITCODE
    $ErrorActionPreference = $prevErrorPref
    if ($searchExitCode -ne 0) {
        Write-Host ("##vso[task.logissue type=warning]Unable to search the diff for iOS/tvOS code (git exit code {0}). Enabling UIKitTestsRequired." -f $searchExitCode)
        $scopeVariables['UIKitTestsRequired'] = $true
        $global:LASTEXITCODE = 0
    } elseif ($uikitFiles.Count -gt 0) {
        Write-Host "Changed lines in '$($uikitFiles[0])' match iOS/tvOS code for UIKitTestsRequired."
        $scopeVariables['UIKitTestsRequired'] = $true
    }
}

# Maintainers force the iOS stage on a PR with this label. The labels are read anonymously; if that
# fails the label is ignored rather than enabling iOS on every PR, since a rerun picks it up.
$uikitLabel = 'ci/run-ios'
if (-not $scopeVariables['UIKitTestsRequired'] -and $env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER) {
    $repository = if ($env:BUILD_REPOSITORY_NAME) { $env:BUILD_REPOSITORY_NAME } else { 'unoplatform/uno' }
    try {
        $labels = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/issues/$($env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER)/labels" -Headers @{ 'User-Agent' = 'uno-ci-test-scope'; 'Accept' = 'application/vnd.github+json' } -TimeoutSec 20
        if (@($labels | Where-Object { $_.name -eq $uikitLabel }).Count -gt 0) {
            Write-Host "The PR has the '$uikitLabel' label: enabling UIKitTestsRequired."
            $scopeVariables['UIKitTestsRequired'] = $true
        }
    } catch {
        Write-Host "##vso[task.logissue type=warning]Unable to read the PR's labels, so '$uikitLabel' is ignored: $($_.Exception.Message)"
    }
}

if (-not $scopeVariables['UIKitTestsRequired']) {
    Write-Host "No iOS/tvOS paths, changed lines or '$uikitLabel' label: Tests - iOS is skipped on this PR and runs on master. Add the label and rerun the pipeline to run it."
}

Publish-TestScopes -Values $scopeVariables
