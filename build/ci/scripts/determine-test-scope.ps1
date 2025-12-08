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
    TemplateTestsRequired = $false
    ScreenshotsRequired  = $false
}

$patterns = @{
    RequireNativeAndroid = [regex]'(?i)^.*\.android\.cs$'
    RequireNativeIos     = [regex]'(?i)^.*\.(ios|uikit)\.cs$'
    RequireNativeWasm    = [regex]'(?i)^.*\.wasm\.cs$'
    SkiaScreenshots      = [regex]'(?i)^.*\.skia\.cs$'
    TemplateTestsRequired = [regex]'(?i)(?:^build/|\.csproj$|\.props$|\.targets$|^src/uno\.sdk/|^src/.*devserver.*|^src/.*remotecontrol.*)'
}

$netcoreMobileProjectPattern = [regex]'(?i)\.netcoremobile\.csproj$'
$wasmProjectPattern = [regex]'(?i)\.wasm\.csproj$'

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

    if ($netcoreMobileProjectPattern.IsMatch($normalized)) {
        if (-not $scopeVariables.RequireNativeAndroid) {
            Write-Host "File '$normalized' touches netcoremobile csproj; enabling RequireNativeAndroid."
            $scopeVariables.RequireNativeAndroid = $true
        }
        if (-not $scopeVariables.RequireNativeIos) {
            Write-Host "File '$normalized' touches netcoremobile csproj; enabling RequireNativeIos."
            $scopeVariables.RequireNativeIos = $true
        }
    }

    if (-not $scopeVariables.RequireNativeWasm -and $wasmProjectPattern.IsMatch($normalized)) {
        Write-Host "File '$normalized' touches wasm csproj; enabling RequireNativeWasm."
        $scopeVariables.RequireNativeWasm = $true
    }
}

$scopeVariables['ScreenshotsRequired'] = $scopeVariables.RequireNativeAndroid -or $scopeVariables.RequireNativeIos -or $scopeVariables.RequireNativeWasm -or $scopeVariables.SkiaScreenshots

Publish-TestScopes -Values $scopeVariables
