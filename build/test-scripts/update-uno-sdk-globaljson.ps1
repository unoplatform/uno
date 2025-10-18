# This script updates the Uno SDK version in all global.json files under the current tree.
# It expects the environment variable NBGV_SemVer2 to be set by the caller (pipeline or shell).

param(
    [string]$Version = $env:NBGV_SemVer2
)

if (-not $Version) {
    Write-Error "NBGV_SemVer2 is not set and no version parameter was provided."
    exit 1
}

# replace the uno.sdk field value in global.json, recursively in all folders
Get-ChildItem -Recurse -Filter global.json | ForEach-Object {
    $globalJsonfilePath = $_.FullName

    Write-Host "Updated $globalJsonfilePath with $Version"

    $globalJson = (Get-Content $globalJsonfilePath) -replace '^\s*//.*' | ConvertFrom-Json
    $globalJson.'msbuild-sdks'.'Uno.Sdk.Private' = $Version
    $globalJson | ConvertTo-Json -Depth 100 | Set-Content $globalJsonfilePath
}
