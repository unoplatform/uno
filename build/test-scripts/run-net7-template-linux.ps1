Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
    }
}

$default = @('-v', 'n', "-p:RestoreConfigFile=$env:NUGET_CI_CONFIG", '-p:EnableWindowsTargeting=true')

$debug = $default + '-c' + 'Debug'
$release = $default + '-c' + 'Release'

# WinUI
cd src/SolutionTemplate

# replace the uno.sdk field value in global.json, recursively in all folders
Get-ChildItem -Recurse -Filter global.json | ForEach-Object {
    
    $globalJsonfilePath = $_.FullName;

    Write-Host "Updated $globalJsonfilePath with $env:GITVERSION_SemVer"

    $globalJson = (Get-Content $globalJsonfilePath) -replace '^\s*//.*' | ConvertFrom-Json
    $globalJson.'msbuild-sdks'.'Uno.Sdk' = $env:GITVERSION_SemVer
    $globalJson | ConvertTo-Json -Depth 100 | Set-Content $globalJsonfilePath
}

$projects =
@(
    # 5.0 and earlier
    @("UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Wasm/UnoAppWinUILinuxValidation.Wasm.csproj", ""),
    @("UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Gtk/UnoAppWinUILinuxValidation.Skia.Gtk.csproj", ""),
    @("UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer.csproj", ""),

    # 5.1 Blank
    @("5.1/uno51blank/uno51blank.Skia.Gtk/uno51blank.Skia.Gtk.csproj", ""),
    @("5.1/uno51blank/uno51blank.Skia.Linux.FrameBuffer/uno51blank.Skia.Linux.FrameBuffer.csproj", ""),
    @("5.1/uno51blank/uno51blank.Skia.WPF/uno51blank.Skia.WPF.csproj", ""),
    @("5.1/uno51blank/uno51blank.Wasm/uno51blank.Wasm.csproj", ""),

    # 5.1 Recommended
    @("5.1/uno51recommended/uno51recommended.Skia.Gtk/uno51recommended.Skia.Gtk.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Skia.Linux.FrameBuffer/uno51recommended.Skia.Linux.FrameBuffer.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Skia.WPF/uno51recommended.Skia.WPF.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Wasm/uno51recommended.Wasm.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Server/uno51recommended.Server.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.Tests/uno51recommended.Tests.csproj", ""),
    @("5.1/uno51recommended/uno51recommended.UITests/uno51recommended.UITests.csproj", ""),

    # 5.2 Blank
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", ""),

    # 5.2 Uno Lib
    @("5.2/uno52Lib/uno52Lib.csproj", ""),

    # 5.2 Uno SingleProject Lib
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", ""),

    # 5.2 Uno App with Library reference
    @("5.2/uno52AppWithLib/uno52AppWithLib/uno52AppWithLib.csproj", "")

    ## Note for contributors
    ##
    ## When adding new template versions, create them in a separate version named folder
    ## using all the specific features that can be impacted by the use of the Uno.SDK
);

for($i = 0; $i -lt $projects.Length; $i++)
{
    $projectPath=$projects[$i][0];
    $projectOptions=$projects[$i][1];

    Write-Host "Building Debug $projectPath with $projectOptions"
    dotnet build $debug "$projectPath" $projectOptions
    Assert-ExitCodeIsZero

    dotnet clean $debug

    Write-Host "Building Release $projectPath with $projectOptions"
    dotnet build $release "$projectPath" $projectOptions
    Assert-ExitCodeIsZero

    dotnet clean $release
}
