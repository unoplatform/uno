# install-msix.ps1 — Remove any existing SamplesApp and install the built MSIX
#
# Usage: pwsh -File install-msix.ps1 -RepoRoot <path-to-repo>

param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

$appPackagesDir = Join-Path $RepoRoot "src\SamplesApp\SamplesApp.Windows\AppPackages"

# --- Step 1: Remove existing SamplesApp ---
$existing = Get-AppxPackage -Name '*SamplesApp*' -ErrorAction SilentlyContinue
foreach ($pkg in $existing) {
    Write-Host "Removing existing package: $($pkg.PackageFullName)"
    Remove-AppxPackage -Package $pkg.PackageFullName
}

# --- Step 2: Find the MSIX bundle (preferred) or MSIX ---
$msixBundle = Get-ChildItem -Path $appPackagesDir -Filter "*.msixbundle" -Recurse -ErrorAction SilentlyContinue |
    Sort-Object -Property LastWriteTime -Descending |
    Select-Object -First 1
if (-not $msixBundle) {
    $msixBundle = Get-ChildItem -Path $appPackagesDir -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike '*scale*' -and $_.Name -notlike 'Microsoft.*' } |
        Sort-Object -Property LastWriteTime -Descending |
        Select-Object -First 1
}

if (-not $msixBundle) {
    throw "No MSIX package found in $appPackagesDir. Did the build succeed?"
}

# --- Step 3: Install ---
Write-Host "Installing: $($msixBundle.FullName)"
Add-AppxPackage -Path $msixBundle.FullName -ForceApplicationShutdown
Write-Host "MSIX installed successfully."
Write-Host "App execution alias 'unosamplesapp.exe' is now available."
