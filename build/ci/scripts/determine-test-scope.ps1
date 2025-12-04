param(
    [string]$DefaultTargetBranch = 'refs/heads/master'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scopeVariables = [ordered]@{
    RequireNativeAndroid = $false
    RequireNativeIos     = $false
    RequireNativeWasm    = $false
    SkiaScreenshots      = $false
    ScreenshotsRequired  = $false
}

$patterns = @{
    RequireNativeAndroid = [regex]'(?i)^.*\.android\.cs$'
    RequireNativeIos     = [regex]'(?i)^.*\.(ios|uikit)\.cs$'
    RequireNativeWasm    = [regex]'(?i)^.*\.wasm\.cs$'
    SkiaScreenshots      = [regex]'(?i)^.*\.skia\.cs$'
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
    param([hashtable]$Values)

    foreach ($key in $Values.Keys) {
        Set-TestScopeVariable -Name $key -Value $Values[$key]
    }
}

function Enable-AllScopes {
    param([hashtable]$Values)

    foreach ($key in $Values.Keys) {
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

try {
    git fetch origin $targetRef --depth=200 | Out-Null
}
catch {
    Write-Warning ("Unable to fetch {0}: {1}. Falling back to enabling all native test scopes." -f $targetRef, $_.Exception.Message)
    Enable-AllScopes -Values $scopeVariables
    Publish-TestScopes -Values $scopeVariables
    return
}

$diffArgs = @('--name-only', 'FETCH_HEAD...HEAD')
$changedFiles = git diff @diffArgs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

if (-not $changedFiles) {
    Publish-TestScopes -Values $scopeVariables
    return
}

foreach ($file in $changedFiles) {
    $trimmed = $file.Trim()
    foreach ($scope in $patterns.GetEnumerator()) {
        if (-not $scopeVariables[$scope.Key] -and $scope.Value.IsMatch($trimmed)) {
            Write-Host "File '$trimmed' matches pattern for $($scope.Key)."
            $scopeVariables[$scope.Key] = $true
        }
    }
}

$scopeVariables['ScreenshotsRequired'] = $scopeVariables.RequireNativeAndroid -or $scopeVariables.RequireNativeIos -or $scopeVariables.RequireNativeWasm -or $scopeVariables.SkiaScreenshots

Publish-TestScopes -Values $scopeVariables
