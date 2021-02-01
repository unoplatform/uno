# From SkiaSharp (https://github.com/mono/SkiaSharp/blob/master/scripts/install-tizen.ps1)
#
# Tizen has issues on DevOps:
#  - https://developer.tizen.org/forums/sdk-ide/cli-installer-v3.3-failing-on-azure-devops
#  - https://developercommunity.visualstudio.com/content/problem/661596/the-updated-path-doesnt-kick-in.html

Param(
    [string] $Version = "3.7",
    [string] $InstallDestination = $null
)

$ErrorActionPreference = 'Stop'

$HOME_DIR = if ($env:HOME) { $env:HOME } else { $env:USERPROFILE }

if ($IsMacOS) {
    $platform = "macos-64"
    $ext = "bin"
} elseif ($IsLinux) {
    $platform = "ubuntu-64"
    $ext = "bin"
} else {
    $platform = "windows-64"
    $ext = "exe"
}

$url = "http://download.tizen.org/sdk/Installer/tizen-studio_${Version}/web-cli_Tizen_Studio_${Version}_${platform}.${ext}"

$ts = Join-Path "$HOME_DIR" "tizen-studio"
if ($InstallDestination) {
    $ts = $InstallDestination
}
Write-Host "Install destination is '$ts'..."

$tsTemp = Join-Path "$HOME_DIR" "tizen-temp"
$install = Join-Path "$tsTemp" "tizen-install.$ext"
$packages = "MOBILE-4.0,MOBILE-4.0-NativeAppDevelopment"

# download
Write-Host "Downloading SDK to '$install'..."
New-Item -ItemType Directory -Force -Path "$tsTemp" | Out-Null
(New-Object System.Net.WebClient).DownloadFile("$url", "$install")

# validation
Write-Host "Validating Java install..."
Write-Host "JAVA_HOME is: $env:JAVA_HOME"
Write-Host "PATH contains JAVA_HOME: $($env:PATH.Contains("$env:JAVA_HOME"))"
& "java" -version

# install
Write-Host "Installing SDK to '$ts'..."
if ($IsMacOS -or $IsLinux) {
    & "bash" "$install" --accept-license --no-java-check "$ts"
} else {
    & "$install" --accept-license --no-java-check "$ts"
}

# install packages
Write-Host "Installing Additional Packages: '$packages'..."
$packMan = Join-Path (Join-Path "$ts" "package-manager") "package-manager-cli.${ext}"
if ($IsMacOS -or $IsLinux) {
    & "bash" "$packMan" install --no-java-check --accept-license "$packages"
} else {
    & "$packMan" install --no-java-check --accept-license "$packages"
}

# make sure that Tizen Studio is in TIZEN_STUDIO_HOME
Write-Host "##vso[task.setvariable variable=TIZEN_STUDIO_HOME;]$ts";

exit $LASTEXITCODE