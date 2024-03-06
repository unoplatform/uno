Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
    }
}

$default = @('-v', 'detailed', "-p:RestoreConfigFile=$env:NUGET_CI_CONFIG", '-p:EnableWindowsTargeting=true')

$debug = $default + '-c' + 'Debug'
$release = $default + '-c' + 'Release'

# WinUI
cd src/SolutionTemplate

$projects =
@(
    # 5.0 and earlier
    @("UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Wasm/UnoAppWinUILinuxValidation.Wasm.csproj", ""),
    @("UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Gtk/UnoAppWinUILinuxValidation.Skia.Gtk.csproj", ""),
    @("UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer.csproj", ""),

    # 5.1 Blank
    @("5.1/uno51blank/uno51blank.Skia.Gtk/uno51blank.Skia.Gtk.csproj", ""),
    @("5.1/uno51blank/uno51blank.Skia.Linux.FrameBuffer/uno51blank.Skia.Linux.FrameBuffer.csproj", ""),
    @("5.1/uno51blank/uno51blank.Skia.Wpf/uno51blank.Skia.Wpf.csproj", ""),
    @("5.1/uno51blank/uno51blank.Skia.Wasm/uno51blank.Skia.Wasm.csproj", ""),

    # 5.1 Recommended
    @("5.1/uno51recommended/uno51recommended.Skia.Gtk/uno51recommended.Skia.Gtk.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Skia.Linux.FrameBuffer/uno51recommended.Skia.Linux.FrameBuffer.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Skia.Wpf/uno51recommended.Skia.Wpf.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Skia.Wasm/uno51recommended.Skia.Wasm.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Server/uno51recommended.Server.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Tests/uno51recommended.Tests.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.UITests/uno51recommended.UITests.csproj", "")
);

for($i = 0; $i -lt $projects.Length; $i++)
{
    $projectPath=$dotnetBuildConfigurations[$i][0];
    $projectOptions=$dotnetBuildConfigurations[$i][0];

    Write-Host "Building Debug $projectPath with $projectOptions"
    dotnet build $debug "$projectPath" $projectOptions

    Write-Host "Building Release $projectPath with $projectOptions"
    dotnet build $release "$projectPath" $projectOptions
}
