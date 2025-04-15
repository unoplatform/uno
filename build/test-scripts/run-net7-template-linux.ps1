param(
    $TestGroup
)

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

    Write-Host "Updated $globalJsonfilePath with $env:NBGV_SemVer2"

    $globalJson = (Get-Content $globalJsonfilePath) -replace '^\s*//.*' | ConvertFrom-Json
    $globalJson.'msbuild-sdks'.'Uno.Sdk.Private' = $env:NBGV_SemVer2
    $globalJson | ConvertTo-Json -Depth 100 | Set-Content $globalJsonfilePath
}

$projects =
@(
    # 5.0 and earlier
    @(0, "UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Wasm/UnoAppWinUILinuxValidation.Wasm.csproj", @(), @()),
    @(0, "UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer.csproj", @(), @()),

    # 5.1 Blank
    @(0, "5.1/uno51blank/uno51blank.Skia.Linux.FrameBuffer/uno51blank.Skia.Linux.FrameBuffer.csproj", @(), @()),
    @(0, "5.1/uno51blank/uno51blank.Skia.WPF/uno51blank.Skia.WPF.csproj", @(), @()),
    @(0, "5.1/uno51blank/uno51blank.Wasm/uno51blank.Wasm.csproj", @(), @()),

    # 5.1 Recommended
    @(1, "5.1/uno51recommended/uno51recommended.Skia.Linux.FrameBuffer/uno51recommended.Skia.Linux.FrameBuffer.csproj", @(), @()),
    @(1, "5.1/uno51recommended/uno51recommended.Skia.WPF/uno51recommended.Skia.WPF.csproj", @(), @()),
    @(1, "5.1/uno51recommended/uno51recommended.Wasm/uno51recommended.Wasm.csproj", @(), @()),
    @(1, "5.1/uno51recommended/uno51recommended.Server/uno51recommended.Server.csproj", @(), @()),
    @(1, "5.1/uno51recommended/uno51recommended.Tests/uno51recommended.Tests.csproj", @(), @()),
    @(1, "5.1/uno51recommended/uno51recommended.UITests/uno51recommended.UITests.csproj", @(), @()),

    # 5.2 Blank
    @(1, "5.2/uno52blank/uno52blank/uno52blank.csproj", @(), @()),

    # 5.2 Blank SkiaSharp 3
    @(1, "5.2/uno52blank/uno52blank/uno52blank.csproj", @("-p:SkiaSharpVersion=3.0.0-preview.3.1"), @()),

    # 5.2 Blank GLCanvas
    @(1, "5.2/uno52blank/uno52blank/uno52blank.csproj", @("-p:UnoFeatures=GLCanvas"), @()),

    # 5.2 Uno Lib
    @(1, "5.2/uno52Lib/uno52Lib.csproj", @(), @()),

    # 5.2 Uno NuGet Lib
    @(2, "5.2/uno52NuGetLib/uno52NuGetLib.csproj", @(), @()),

    # 5.2 Uno SingleProject Lib
    @(2, "5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @(), @()),

    # 5.2 Uno App with Library reference
    @(2, "5.2/uno52AppWithLib/uno52AppWithLib/uno52AppWithLib.csproj", @(), @()),

    # 5.3 Blank with net9
    @(3, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @(), @()),
    @(3, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-browserwasm", "-p:UseArtifactsOutput=true", "-p:UnoXamlResourcesTrimming=true"), @()),

    # 5.3 lib
    @(3, "5.3/uno53net9Lib/uno53net9Lib.csproj", @(), @()),

    # 5.3 Uno App with Library reference
    @(3, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @(), @()),

    # 5.3 blank publish testing
    # Disabled for LXD setup issues
    # @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f:net9.0-desktop", "-p:SelfContained=true", "-p:PackageFormat=snap"), @("Publish"))

    # 5.6 Android/ios/Wasm+Skia
    @(3, "5.6/uno56droidioswasmskia/uno56droidioswasmskia/uno56droidioswasmskia.csproj", @(), @()),

    # 5.6 Win32+Skia
    @(3, "5.6/uno56net9win32/uno56net9win32/uno56net9win32.csproj", @(), @()),

    ## Note for contributors
    ##
    ## When adding new template versions, create them in a separate version named folder
    ## using all the specific features that can be impacted by the use of the Uno.SDK

    # Empty marker to allow new tests lines to end with a comma
    @()
);

for($i = 0; $i -lt $projects.Length; $i++)
{
    # Skip the end marker to help for new tests authoring
    if ($projects[$i].Length -eq 0)
    {
        continue
    }

    $projectTestGroup=$projects[$i][0];
    $projectPath=$projects[$i][1];
    $projectParameters=$projects[$i][2];

    $buildOptions=$projects[$i][3];
    $usePublish = $buildOptions -contains "Publish"

    if ($TestGroup -ne $projectTestGroup)
    {
        Write-Host "Skipping test $projectPath for group $projectTestGroup"
        continue
    }

    if(!$usePublish)
    {
        Write-Host "Building Debug $projectPath with $projectParameters"
        dotnet build $debug "$projectPath" $projectParameters -bl:binlogs/$projectPath/$i/debug.binlog
        Assert-ExitCodeIsZero

        dotnet clean $debug "$projectPath"
    }

    $dotnetCommand = $usePublish ? "publish" : "build"

    # Disable most costly features to speed up the build
    $extraArgs=@(
        "-p:RunAOTCompilation=false",
        "-p:MtouchUseLlvm=false",
        "-p:MtouchLink=none",
        "-p:WasmShellILLinkerEnabled=false",
        "-p:UseInterpreter=true",
        "-p:_IsDedupEnabled=false",
        "-p:MtouchInterpreter=all"
    );


    Write-Host "Building Release $projectPath with $projectParameters"
    dotnet $dotnetCommand $release "$projectPath" $projectParameters $extraArgs -bl:binlogs/$projectPath/$i/release.binlog
    Assert-ExitCodeIsZero

    dotnet clean $release "$projectPath"
}
