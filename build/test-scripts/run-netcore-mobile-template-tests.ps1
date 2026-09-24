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

function Assert-OutputFiles()
{
    param ([string]$projectPath, [string[]]$expectPresent, [string[]]$expectAbsent)

    # Verifies what a build actually shipped, so a case can assert both that a payload is present and that
    # another was left out.

    $projectDir = Split-Path -Parent $projectPath

    foreach ($relative in $expectPresent)
    {
        $matches = @(Get-ChildItem -Path $projectDir -Filter $relative -Recurse -File -ErrorAction SilentlyContinue)
        if ($matches.Length -eq 0)
        {
            throw "Expected '$relative' in the output of $projectPath, but it is missing."
        }

        Write-Host "OK: '$relative' present in $projectPath"
    }

    foreach ($relative in $expectAbsent)
    {
        $matches = @(Get-ChildItem -Path $projectDir -Filter $relative -Recurse -File -ErrorAction SilentlyContinue)
        if ($matches.Length -ne 0)
        {
            throw "Did not expect '$relative' in the output of $projectPath, found: $($matches[0].FullName)"
        }

        Write-Host "OK: '$relative' absent from $projectPath"
    }
}

function CleanupTree()
{
    git clean -fdx -e *.binlog
}

$default = @('/ds', '/v:m', '/p:UseDotNetNativeToolchain=false', '/p:PackageCertificateKeyFile=')

if ($IsWindows) 
{
    $msbuild = vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
}

$debug = $default + '/p:Configuration=Debug' + '/r'
$release = $default + '/p:Configuration=Release' + '/r'

cd src/SolutionTemplate

if ( ($TestGroup -eq 0) -and ($env:UWPBuildEnabled -eq 'True') )
{
    ## Configurations are split to work around UWP not building with .NET new
    $dotnetBuildConfigurations =
    @(
        @("Skia.Linux.FrameBuffer", "", "")
    )

    $dotnetBuildNet6Configurations =
    @(
        @("Server", "", ""),
        @("Skia.Linux.FrameBuffer", "", "")
    )

    # WinUI - Default
    pushd UnoAppWinUI
    for($i = 0; $i -lt $dotnetBuildNet6Configurations.Length; $i++)
    {
        $platform=$dotnetBuildNet6Configurations[$i][0];
        & dotnet build -c Debug $default $dotnetBuildNet6Configurations[$i][1] $dotnetBuildNet6Configurations[$i][2] "UnoAppWinUI.$platform\UnoAppWinUI.$platform.csproj" -bl:../binlogs/UnoAppWinUI.$platform/debug/$i/msbuild.binlog
        Assert-ExitCodeIsZero
    }

    if ($IsWindows) 
    {
        # Server project build (merge with above loop when .App folder is removed)
        & dotnet build -c Debug $default "UnoAppWinUI.Server\UnoAppWinUI.Server.csproj"

        # Build with msbuild because of https://github.com/microsoft/WindowsAppSDK/issues/1652
        # force targetframeworks until we can get WinAppSDK to build with `dotnet build`
        & $msbuild $debug "/p:Platform=x86" "UnoAppWinUI.Windows\UnoAppWinUI.Windows.csproj" "/p:TargetFrameworks=net11.0-windows10.0.19041;TargetFramework=net11.0-windows10.0.19041" "/bl:../binlogs/UnoAppWinUI.Windows/debug/$i/msbuild.binlog"
        Assert-ExitCodeIsZero
    }

    CleanupTree

    popd

    if ($IsWindows) 
    {
        # Uno Library
        # Mobile is removed for now, until we can get net7 supported by msbuild/VS 17.4
        $responseFile = @(
            "$debug",
            "/t:pack",
            "MyUnoLib\MyUnoLib.csproj",
            "/p:TargetFrameworks=""net11.0-windows10.0.19041;net11.0"""
        )
        $responseFile | Out-File -FilePath "build.rsp" -Encoding ASCII

        & $msbuild "@build.rsp"
        Assert-ExitCodeIsZero

        if (!$IsWindows)
        {
            # disabled on windows until android 35 is supported in the installed VS instance

            # Uno Cross-Runtime Library
            & $msbuild $debug /t:Pack MyCrossRuntimeLib\MyCrossRuntimeLib.sln -bl:binlogs/MyCrossRuntimeLib/msbuild.binlog
            Assert-ExitCodeIsZero
        }

        #
        # Uno Library with assets, Validate assets count
        #
        # Mobile is removed for now, until we can get net7 supported by msbuild/VS 17.4
        $responseFile = @(
            "$debug",
            "/t:pack",
            "/p:IncludeContentInPack=false",
            "MyUnoLib2\MyUnoLib2.csproj",
            "-bl",
            "/p:TargetFrameworks=""net11.0-windows10.0.19041;net11.0"""
        )
        $responseFile | Out-File -FilePath "build.rsp" -Encoding ASCII

        & $msbuild "@build.rsp"
        Assert-ExitCodeIsZero

        mv MyUnoLib2\Bin\Debug\MyUnoLib2.1.0.0.nupkg MyUnoLib2\Bin\Debug\MyUnoLib2.1.0.0.zip
        Expand-Archive -LiteralPath MyUnoLib2\Bin\Debug\MyUnoLib2.1.0.0.zip -DestinationPath MyUnoLib2Extract

        $assetsCount = Get-ChildItem MyUnoLib2Extract\ -Filter MyTestAsset01.txt -Recurse -File | Measure-Object | %{$_.Count}

        #if ($assetsCount -ne 6) # Restore when mobile validation is available
        if ($assetsCount -ne 2)
        {
            throw "Not enough assets in the package."
        }
    }

    CleanupTree
}

## Tests Per versions of uno

# TODO Uno (7.0 dependents): drop this switch.
# Uno.UI.HotDesign, and every Uno-family feature package below, are still compiled against the
# pre-7.0 `Uno` assembly, which no longer exists after the Uno.WinRT rename. The jobs that run
# this script are disabled in .azure-devops-tests-templates.yml for the same reason; both come
# back together once 7.0 builds of those packages are published.
$noPreRenamePackages = '-p:UnoDisableHotDesign=true'

if ($IsWindows)
{
    $default = @('-v:m', '-p:EnableWindowsTargeting=true', $noPreRenamePackages)
}
else
{
    $default = @('-v:m', '-p:AotAssemblies=false', $noPreRenamePackages)
}

$debug = $default + '-p:Configuration=Debug'
$release = $default + '-p:Configuration=Release'

& $env:BUILD_SOURCESDIRECTORY/build/test-scripts/update-uno-sdk-globaljson.ps1

# TODO Uno (7.0 dependents): restore the full feature set.
# Material, Extensions, Toolkit, CSharpMarkup and MVUX all resolve to packages built against the
# pre-7.0 `Uno` assembly; Svg is the only one of the set that comes from this repository. These
# come back per-feature as each dependent ships a 7.0 build -- not necessarily all at once.
$sdkFeatures = "-p:UnoFeaturesOverride=Svg";

$projects =
@(
    # 5.3 Uno App with net11
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-browserwasm"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-browserwasm", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-browserwasm", "-p:UseArtifactsOutput=true", "-p:UnoXamlResourcesTrimming=true"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-desktop"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-desktop", $sdkFeatures), @("macOS", "NetCore")),

    # Android heads must build through `dotnet build`: the templates strip the android TFM
    # when MSBuildRuntimeType is 'Full'.
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-android"), @("macOS", "NetCore")),

    # The android head with the SDK features is disabled until Toolkit/Extensions ship builds
    # against Uno 7.0. Their current android assemblies were compiled when DependencyObject was
    # an interface, so those types list it in their interface list while deriving from Object.
    # Now that it is a class carrying a finalizer, ILLink maps DependencyObject.Finalize as a base
    # of Object.Finalize, which already is its base - MarkBaseMethods then recurses until the
    # stack overflows. Those types cannot work on 7.0 regardless; they need a rebuild.
    # @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-android", $sdkFeatures), @("macOS", "NetCore")),

    # Default mode for the template is WindowsAppSDKSelfContained=true, which requires specifying a target platform.
    @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-p:Platform=x86" , "-p:TargetFramework=net11.0-windows10.0.19041"), @()),

    # 5.3 Library
    @(2, "5.3/uno53net9Lib/uno53net9Lib.csproj", @(), @("macOS", "NetCore")),

    # Publishing validation
    @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-desktop", "-p:TargetFrameworks=net11.0-desktop", "-p:PackageFormat=app", "-r", "osx-x64", "-p:RuntimeIdentifiers=osx-x64"), @("OnlyMacOS", "NetCore", "Publish")),

    # Publish with no debug symbols validation
    @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net11.0-desktop", "-p:TargetFrameworks=net11.0-desktop", "-r", "win-x64", "-p:DebugSymbols=false", "-p:DebugType=None"), @("NetCore", "Publish")),

    # Publish with NativeAOT and *run*
    @(2, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-desktop", "-r", "osx-x64", "-p:PublishAot=true"), @("OnlyMacOS", "NetCore", "Publish"),
        @("5.6/uno56netcurrent/uno56netcurrent/bin/Release/net11.0-desktop/osx-x64/publish/uno56netcurrent"), @("--exit")),

    # Renderer selection and its native payload. 'skia' is implied only when the app names no renderer, and the
    # wgpu native ships only where the app can actually reach the WebGPU backend, so each case asserts what
    # landed in the output rather than just that the build succeeded.
    #
    # No renderer named: skia is implied, the WebGpu package is never referenced.
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-desktop"), @("NetCore"),
        @(), @(), @("libSkiaSharp.dll"), @("webgpu.dll")),

    # Both named, but nothing in the app goes near WebGPU: the backend is unreachable, so its native is left out.
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-desktop", "-p:UnoFeaturesOverride=Skia%3BWebGpu"), @("NetCore"),
        @(), @(), @("libSkiaSharp.dll"), @("webgpu.dll")),

    # Both named and the app's own code names the backend: it is reachable, so the native ships.
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-desktop", "-p:UnoFeaturesOverride=Skia%3BWebGpu", "-p:CustomBeforeMicrosoftCommonTargets=$env:BUILD_SOURCESDIRECTORY\build\test-scripts\webgpu-probe\InjectProbe.targets"), @("NetCore"),
        @(), @(), @("webgpu.dll", "libSkiaSharp.dll"), @()),

    # WebGPU named alone: skia is NOT implied, so the app is SkiaSharp-free and the native ships.
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-desktop", "-p:UnoFeaturesOverride=WebGpu"), @("NetCore"),
        @(), @(), @("webgpu.dll"), @("libSkiaSharp.dll")),

    # 5.6 net-current runtime folder validation
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @(), @("macOS", "NetCore")),
    
    # 5.6 net-current with XAML trimming validation - desktop
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-desktop", "-p:UnoXamlResourcesTrimming=true", "-p:PublishTrimmed=true", "-r", "win-x64"), @("NetCore", "Publish")),
    
    # 5.6 net-current with XAML trimming validation - wasm
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net11.0-browserwasm", "-p:UnoXamlResourcesTrimming=true", "-p:WasmShellILLinkerEnabled=true"), @("macOS", "NetCore", "Publish")),

    # 5.6 multi-platform template - covers the Android application head (Platforms/Android).
    # The app carries a PackageReference on Uno56NugetLibrary, so that package must be packed
    # into the "Solution Packages" feed before the app can restore.
    @(3, "5.6/uno56droidioswasmskia/Uno56NugetLibrary/Uno56NugetLibrary.csproj", @("-p:PackageOutputPath=$env:BUILD_SOURCESDIRECTORY\src\PackageCache"), @("macOS", "NetCore", "CleanNugetTemp", "NoBuildClean")),
    @(3, "5.6/uno56droidioswasmskia/uno56droidioswasmskia/uno56droidioswasmskia.csproj", @("-f", "net11.0-android"), @("macOS", "NetCore")),

    # Ensure that build can happen even if a RID is specified
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net11.0"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net11.0-browserwasm"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net11.0-desktop"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net11.0-android"), @("macOS", "NetCore")),

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
    $projectOptions=$projects[$i][2];
    $buildOptions=$projects[$i][3];
    $runCommand=$projects[$i][4];
    $runOptions=$projects[$i][5];
    $expectPresent=$projects[$i][6];
    $expectAbsent=$projects[$i][7];
    $runOnMacOS = $buildOptions -contains "macOS"
    $runOnlyOnMacOS = $buildOptions -contains "OnlyMacOS"
    $buildWithNetCore = $buildOptions -contains "NetCore"
    $usePublish = $buildOptions -contains "Publish"
    $cleanNugetCache = $buildOptions -contains "CleanNugetTemp"
    $NoBuildClean = $buildOptions -contains "NoBuildClean"

    if ($TestGroup -ne $projectTestGroup)
    {
        Write-Host "Skipping test $projectPath for group $projectTestGroup"
        continue
    }

    if ($IsMacOS -and -not $runOnMacOS -and -not $runOnlyOnMacOS)
    {
        Write-Host "Skipping on macOS: $projectPath with $projectOptions"
        continue
    }

    if (!$IsMacOS -and $runOnlyOnMacOS)
    {
        Write-Host "Skipping on Windows: $projectPath with $projectOptions"
        continue
    }

    if (($expectPresent.Length -gt 0) -or ($expectAbsent.Length -gt 0))
    {
        # The assertions read the output tree, so a previous case's payload must not still be sitting in it.
        $projectBin = Join-Path (Split-Path -Parent $projectPath) "bin"
        if (Test-Path $projectBin) { Remove-Item -Recurse -Force $projectBin }
    }

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

    if ($buildWithNetCore)
    {
        if(!$usePublish)
        {
            Write-Host "NetCore Building Debug $projectPath with $projectOptions"
            Write-Host "Executing: dotnet build $debug ""$projectPath"" $projectOptions -bl"
            dotnet build $debug "$projectPath" $projectOptions -bl:binlogs/$projectPath/$i/debug/msbuild.binlog
            Assert-ExitCodeIsZero

            dotnet clean $debug "$projectPath"
        }

        $dotnetCommand = $usePublish ? "publish" : "build"

        Write-Host "NetCore Building Release $projectPath with $projectOptions"
        Write-Host "Executing: dotnet $dotnetCommand $release ""$projectPath"" $projectOptions $extraArgs -bl"
        dotnet $dotnetCommand $release "$projectPath" $projectOptions $extraArgs -bl:binlogs/$projectPath/$i/release/msbuild.binlog
        Assert-ExitCodeIsZero

        if ($runCommand.Length -gt 0)
        {
            Write-Host "Executing: $runCommand $runOptions"
            & $runCommand $runOptions
            Assert-ExitCodeIsZero
        }

        if (($expectPresent.Length -gt 0) -or ($expectAbsent.Length -gt 0))
        {
            Assert-OutputFiles -projectPath "$projectPath" -expectPresent $expectPresent -expectAbsent $expectAbsent
        }
 
        if(!$NoBuildClean)
        {
            # Cleaning may also remove generated nuget files, even if
            # OutputPath has been overriden, causing dependents to not find
            # the pacage.
            dotnet clean $release $projectOptions "$projectPath"
        }
    }
    else
    {
        if ($IsWindows) 
        {
            Write-Host "MSBuild Building Debug $projectPath with $projectOptions"
            Write-Host "Executing: ""$msbuild"" $debug /r ""$projectPath"" $projectOptions"
            & $msbuild $debug /r "$projectPath" $projectOptions
            Assert-ExitCodeIsZero

            & $msbuild $debug /r /t:Clean "$projectPath" /bl:binlogs/$projectPath/$i/release/msbuild.binlog

            Write-Host "MSBuild Building Release $projectPath with $projectOptions"
            Write-Host "Executing: ""$msbuild"" $release /r ""$projectPath"" $projectOptions $extraArgs /bl"
            & $msbuild $release /r "$projectPath" $projectOptions $extraArgs /bl:binlogs/$projectPath/$i/release/msbuild.binlog
            Assert-ExitCodeIsZero

            if ($runCommand.Length -gt 0)
            {
                Write-Host "Executing: $runCommand $runOptions"
                & $runCommand $runOptions
                Assert-ExitCodeIsZero
            }

            & $msbuild $release /r /t:Clean "$projectPath"
        }
    }

    if($cleanNugetCache)
    {
        # Clean the nuget cache in order to avoid missed lookups when generating test packages
        dotnet nuget locals temp -c
        dotnet nuget locals http-cache -c
    }
}
