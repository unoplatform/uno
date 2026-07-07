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

cd src/SolutionTemplate


& $env:BUILD_SOURCESDIRECTORY/build/test-scripts/update-uno-sdk-globaljson.ps1

$projects =
@(
    # 5.3 Blank with net9
    @(0, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @(), @()),
    @(0, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net10.0-browserwasm", "-p:UseArtifactsOutput=true", "-p:UnoXamlResourcesTrimming=true"), @()),

    # 5.3 lib
    @(1, "5.3/uno53net9Lib/uno53net9Lib.csproj", @(), @()),

    # 5.6 net-current runtime folder validation
    @(1, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @(), @()),
    
    # 5.6 net-current with XAML trimming validation - desktop
    @(1, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net10.0-desktop", "-p:UnoXamlResourcesTrimming=true", "-p:PublishTrimmed=true", "-r", "linux-x64"), @("Publish")),
    
    # 5.6 net-current with XAML trimming validation - wasm
    @(1, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net10.0-browserwasm", "-p:UnoXamlResourcesTrimming=true", "-p:WasmShellILLinkerEnabled=true"), @("Publish")),

    # 5.3 Uno App with Library reference
    @(2, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @(), @()),

    # 5.3 blank publish testing
    # Disabled for LXD setup issues
    # @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f:net9.0-desktop", "-p:SelfContained=true", "-p:PackageFormat=snap"), @("Publish"))

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
    $cleanNugetCache = $buildOptions -contains "CleanNugetTemp"
    $noBuildClead = $buildOptions -contains "NoBuildClean"

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

    if($cleanNugetCache)
    {
        dotnet nuget locals temp -c
        dotnet nuget locals http-cache -c
    }

    if(!$NoBuildClean)
    {
        # Cleaning may also remove generated nuget files, even if
        # OutputPath has been overriden, causing dependents to not find
        # the pacage.
        dotnet clean $release "$projectPath"
    }
}

# Regression guard for issue #23658: overriding SkiaSharpVersion on a desktop head must retarget
# SkiaSharp.NativeAssets.Linux too. The base SkiaSharp package pulls its Win32/macOS native assets
# but not the Linux one, so without the Uno.Sdk injecting it the Linux native stays at the runtime
# packs' floor while managed SkiaSharp moves up — a silent mismatch that crashes Skia Desktop on Linux.
if ($TestGroup -eq '1')
{
    $skiaAlignProject = "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj"
    $skiaAlignVersion = "4.148.0"
    $skiaAlignAssets = "5.6/uno56netcurrent/uno56netcurrent/obj/project.assets.json"

    Write-Host "Validating SkiaSharp.NativeAssets.Linux aligns with an overridden SkiaSharpVersion ($skiaAlignVersion) - issue #23658"
    dotnet restore $skiaAlignProject "-p:SkiaSharpVersion=$skiaAlignVersion" "-p:RestoreConfigFile=$env:NUGET_CI_CONFIG" -p:EnableWindowsTargeting=true -bl:binlogs/skiasharp-linux-alignment/restore.binlog
    Assert-ExitCodeIsZero

    $assets = Get-Content $skiaAlignAssets -Raw
    if ($assets -notmatch [regex]::Escape("SkiaSharp.NativeAssets.Linux/$skiaAlignVersion"))
    {
        throw "SkiaSharp.NativeAssets.Linux did not resolve to $skiaAlignVersion to match the overridden managed SkiaSharp. The Uno.Sdk must inject the Linux native asset for desktop heads (issue #23658)."
    }
    Write-Host "SkiaSharp.NativeAssets.Linux correctly aligned to $skiaAlignVersion"
}
