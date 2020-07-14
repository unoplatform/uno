#
# Imports theme resource Xaml files from the WinUI repo
#

$winui_path = '[REPO_PATH]\controls'
$destination_path_rel = '..\src\Uno.UI\Themes\WinUI\Resources'

$destination_path_abs = Join-Path (Get-Location) $destination_path_rel
Write-Host $destination_path_abs

$themeresource_files = Get-ChildItem -Path $winui_path -Recurse -Filter *.xaml -Name | Where-Object {-Not ($_.Contains("TestUI") -Or $_.Contains("ReleaseTest") -Or $_.Contains("test\") -Or $_.Contains("NEWCONTROL"))}

$themeresource_files | foreach {
    $source_path = Join-Path $winui_path $_
    Copy-Item $source_path -Destination $destination_path_abs
}

$no_files = $themeresource_files.Length

Write-Host "Copied $no_files files"
