# cleanup.ps1 — Uninstall SamplesApp MSIX and optionally clean build artifacts
#
# Usage: pwsh -File cleanup.ps1 [-RepoRoot <path>] [-CleanBuild]

param(
    [Parameter(Mandatory=$false)]
    [string]$RepoRoot = "",

    [switch]$CleanBuild
)

# Covers both flows: Remove-AppxPackage handles the MSIX install, and also deregisters the
# development-mode registration that `winapp run` creates from the build output folder.
$packages = Get-AppxPackage -Name '*SamplesApp*' -ErrorAction SilentlyContinue
if ($packages) {
    foreach ($pkg in $packages) {
        Write-Host "Removing: $($pkg.PackageFullName) (development mode: $($pkg.IsDevelopmentMode))"
        Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction SilentlyContinue
    }
    Write-Host "SamplesApp uninstalled."
} else {
    Write-Host "No SamplesApp packages found."
}

# Optionally clean build artifacts
if ($CleanBuild -and $RepoRoot) {
    $appPackagesDir = Join-Path $RepoRoot "src\SamplesApp\SamplesApp\AppPackages"
    if (Test-Path $appPackagesDir) {
        Write-Host "Removing: $appPackagesDir"
        Remove-Item $appPackagesDir -Recurse -Force
    }
}
