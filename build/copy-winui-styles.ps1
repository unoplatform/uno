#
# Imports theme resource Xaml files from the WinUI repo
#

$winui_path = '[REPO_PATH]\controls'
$destination_path_rel = '..\src\Uno.UI\Themes\WinUI\Resources'
$destination_path_rel_compact = '..\src\Uno.UI\Themes\WinUI\DensityStyles'

$destination_path_abs = Join-Path (Get-Location) $destination_path_rel
$destination_path_abs_compact = Join-Path (Get-Location) $destination_path_rel_compact
Write-Host $destination_path_abs

$themeresource_files = Get-ChildItem -Path $winui_path -Recurse -Filter *.xaml -Name | Where-Object { -Not ($_.Contains("TestUI") -Or $_.Contains("ReleaseTest") -Or $_.Contains("test\") -Or $_.Contains("NEWCONTROL")) }

$themeresource_files | ForEach-Object {
    $source_path = Join-Path $winui_path $_
    
    $file_name = Split-Path $_ -Leaf
    $priority = switch ($file_name) {
        "Common_themeresources.xaml" { "Priority01" }
        "TextControlsCommon_themeresources.xaml" { "Priority01" }
        "RichEditBox_themeresources.xaml" { "Priority02" }
        "PasswordBox_themeresources.xaml" { "Priority02" }
        "TextBox_themeresources.xaml" { "Priority02" }
        "AcrylicBrush_themeresources.xaml" { "Priority02" }
        "MenuFlyout_themeresources.xaml" { "Priority02" }
        "RevealBrush_themeresources.xaml" { "Priority06" }
        Default { "PriorityDefault" }
    }
    $destination_path_priority = Join-Path $destination_path_abs $priority

    $destination_path = If ($source_path.Contains("Compact")) { $destination_path_abs_compact } Else { $destination_path_priority }
    Copy-Item $source_path -Destination $destination_path
}

$no_files = $themeresource_files.Length

Write-Host "Copied $no_files files"
