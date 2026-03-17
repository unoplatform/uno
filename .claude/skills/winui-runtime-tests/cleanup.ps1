# cleanup.ps1 — Uninstall SamplesApp MSIX and optionally clean build artifacts
#
# Usage: pwsh -File cleanup.ps1 [-RepoRoot <path>] [-CleanBuild]

param(
    [Parameter(Mandatory=$false)]
    [string]$RepoRoot = "",

    [switch]$CleanBuild
)

# Uninstall all SamplesApp packages
$packages = Get-AppxPackage -Name '*SamplesApp*' -ErrorAction SilentlyContinue
if ($packages) {
    foreach ($pkg in $packages) {
        Write-Host "Removing: $($pkg.PackageFullName)"
        Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction SilentlyContinue
    }
    Write-Host "SamplesApp uninstalled."
} else {
    Write-Host "No SamplesApp packages found."
}

# Optionally clean build artifacts
if ($CleanBuild -and $RepoRoot) {
    $appPackagesDir = Join-Path $RepoRoot "src\SamplesApp\SamplesApp.Windows\AppPackages"
    if (Test-Path $appPackagesDir) {
        Write-Host "Removing: $appPackagesDir"
        Remove-Item $appPackagesDir -Recurse -Force
    }
}
