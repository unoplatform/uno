#
# Imports theme resource Xaml files from the WinUI repo
#

$winui_path = '[REPO_PATH]\controls'
$destination_path_rel = '..\src\Uno.UI\Themes\WinUI\Resources'
$destination_path_rel_compact = '..\src\Uno.UI\Themes\WinUI\DensityStyles'

$destination_path_abs = Join-Path (Get-Location) $destination_path_rel
$destination_path_abs_compact = Join-Path (Get-Location) $destination_path_rel_compact
Write-Host $destination_path_abs

$themeresource_files = Get-ChildItem -Path $winui_path -Recurse -Filter *.xaml -Name | Where-Object {-Not ($_.Contains("TestUI") -Or $_.Contains("ReleaseTest") -Or $_.Contains("test\") -Or $_.Contains("NEWCONTROL"))}

$themeresource_files | ForEach-Object {
    $source_path = Join-Path $winui_path $_
    $destination_path = If ($source_path.Contains("Compact")) {$destination_path_abs_compact} Else {$destination_path_abs}
    Copy-Item $source_path -Destination $destination_path
}

$no_files = $themeresource_files.Length

Write-Host "Copied $no_files files"
