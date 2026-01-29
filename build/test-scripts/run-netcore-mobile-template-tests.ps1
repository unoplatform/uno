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
        @("Mobile", "-f:net8.0-android", ""), # workaround for https://github.com/xamarin/xamarin-android/issues/7473
        @("Mobile", "-f:net8.0-ios", ""),
        @("Mobile", "-f:net8.0-maccatalyst", ""),
        @("Wasm", "", ""),
        @("Skia.Linux.FrameBuffer", "", "")
    )

    if ($IsWindows) 
    {
        $dotnetBuildConfigurations += , @("Skia.WPF", "", "");
    }

    $dotnetBuildNet6Configurations =
    @(
        @("Mobile", "-f:net8.0-android", ""),
        @("Mobile", "-f:net8.0-ios", ""),
        @("Mobile", "-f:net8.0-maccatalyst", ""),
        @("Wasm", "", ""),
        @("Server", "", ""),
        @("Skia.Linux.FrameBuffer", "", "")
    )

    if ($IsWindows) 
    {
        $dotnetBuildNet6Configurations += , @("Skia.WPF", "", "");
    }

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
        & $msbuild $debug "/p:Platform=x86" "UnoAppWinUI.Windows\UnoAppWinUI.Windows.csproj" "/p:TargetFrameworks=net8.0-windows10.0.19041;TargetFramework=net8.0-windows10.0.19041" "/bl:../binlogs/UnoAppWinUI.Windows/debug/$i/msbuild.binlog"
        Assert-ExitCodeIsZero
    }

    CleanupTree

    popd

    # XAML Trimming build smoke test
    # See https://github.com/unoplatform/uno/issues/9632
    # dotnet publish -c Debug -r win-x64 -p:PublishTrimmed=true -p:SelfContained=true -p:UnoXamlResourcesTrimming=true MyAppXamlTrim\MyAppXamlTrim.Skia.Gtk\MyAppXamlTrim.Skia.Gtk.csproj
    # Assert-ExitCodeIsZero
    # 
    # dotnet run -c Debug --project src\Uno.XamlTrimmingValidator\Uno.XamlTrimmingValidator.csproj -- --hints-file=build\assets\MyAppXamlTrim-hints.txt --target-assembly=MyAppXamlTrim\MyAppXamlTrim.Skia.Gtk\bin\Debug\net6.0\win-x64\publish\Uno.UI.dll
    # Assert-ExitCodeIsZero

    if ($IsWindows) 
    {
        dotnet build MyAppXamlTrim\MyAppXamlTrim.Wasm\MyAppXamlTrim.Wasm.csproj -c Release -p:UnoXamlResourcesTrimming=true -p:WasmShellGenerateCompressedFiles=false -p:WasmShellILLinkerEnabled=true -bl:binlogs/MyAppXamlTrim.Wasm/release/msbuild.binlog
        Assert-ExitCodeIsZero

        dotnet run --project ..\Uno.ResourceTrimmingValidator\Uno.ResourceTrimmingValidator.csproj -- -a (Get-ChildItem MyAppXamlTrim.Wasm.clr -Recurse).FullName -r Strings.en.Resources.upri -x Strings.fr.Resources.upri
        Assert-ExitCodeIsZero

        dotnet run --project ..\Uno.ResourceTrimmingValidator\Uno.ResourceTrimmingValidator.csproj -- -a (Get-ChildItem Uno.UI.clr -Recurse).FullName -r Resources.Strings.en.Resources.upri -r UI.Xaml.DragDrop.Strings.en-US.Resources.upri -x Resources.Strings.cs-CZ.Resources.upri
        Assert-ExitCodeIsZero

        # Uno Library
        # Mobile is removed for now, until we can get net7 supported by msbuild/VS 17.4
        $responseFile = @(
            "$debug",
            "/t:pack",
            "MyUnoLib\MyUnoLib.csproj",
            "/p:TargetFrameworks=""net8.0-windows10.0.19041;net8.0"""
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
            "/p:TargetFrameworks=""net8.0-windows10.0.19041;net8.0"""
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
if ($IsWindows)
{
    $default = @('-v:m', '-p:EnableWindowsTargeting=true')
}
else
{
    $default = @('-v:m', '-p:AotAssemblies=false', '-p:ValidateXcodeVersion=false')
}

$debug = $default + '-p:Configuration=Debug'
$release = $default + '-p:Configuration=Release'

& $env:BUILD_SOURCESDIRECTORY/build/test-scripts/update-uno-sdk-globaljson.ps1

$sdkFeatures = $(If ($IsWindows) {"-p:UnoFeatures=Material%3BExtensions%3BToolkit%3BCSharpMarkup%3BSvg%3BMVUX"} Else { "-p:UnoFeatures=Material%3BToolkit" });

$projects =
@(
    # 5.3 Uno App with net9
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-browserwasm"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-browserwasm", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-browserwasm", "-p:UseArtifactsOutput=true", "-p:UnoXamlResourcesTrimming=true"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-ios"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-ios", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-android"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-android", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-maccatalyst"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-maccatalyst", $sdkFeatures), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-desktop"), @("macOS", "NetCore")),
    @(1, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-desktop", $sdkFeatures), @("macOS", "NetCore")),

    # Default mode for the template is WindowsAppSDKSelfContained=true, which requires specifying a target platform.
    @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-p:Platform=x86" , "-p:TargetFramework=net9.0-windows10.0.19041"), @()),

    # 5.3 Library
    @(2, "5.3/uno53net9Lib/uno53net9Lib.csproj", @(), @("macOS", "NetCore")),

    # Publishing validation
    @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-desktop", "-p:TargetFrameworks=net9.0-desktop", "-p:PackageFormat=app", "-r", "osx-x64", "-p:RuntimeIdentifiers=osx-x64"), @("OnlyMacOS", "NetCore", "Publish")),

    # Publish with no debug symbols validation
    @(2, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-desktop", "-p:TargetFrameworks=net9.0-desktop", "-r", "win-x64", "-p:DebugSymbols=false", "-p:DebugType=None"), @("NetCore", "Publish")),

    # Publish with NativeAOT and *run*
    @(2, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net10.0-desktop", "-r", "osx-x64", "-p:PublishAot=true"), @("OnlyMacOS", "NetCore", "Publish"),
        @("5.6/uno56netcurrent/uno56netcurrent/bin/Release/net10.0-desktop/osx-x64/publish/uno56netcurrent"), @("--exit")),

    # Workaround for: https://github.com/dotnet/android/issues/10423
    # Must happen before trying `dotnet build -r …`
    @(3, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-android"), @("macOS", "NetCore")),

    # Ensure that build can happen even if a RID is specified
    @(3, "5.3/uno53net9blank/uno53net9blank/uno53net9blank.csproj", @("-f", "net9.0-android", "-r", "android-arm64"), @("macOS", "NetCore"))

    # 5.6 Android/ios/Wasm+Skia nuget package (build first before the app)
    @(3, "5.6/uno56droidioswasmskia/Uno56NugetLibrary/Uno56NugetLibrary.csproj", @("-p:PackageOutputPath=$env:BUILD_SOURCESDIRECTORY\src\PackageCache"), @("macOS", "NetCore", "CleanNugetTemp","NoBuildClean")),

    # 5.6 Android/ios/Wasm+Skia
    @(3, "5.6/uno56droidioswasmskia/uno56droidioswasmskia/uno56droidioswasmskia.csproj", @(), @("macOS", "NetCore")),

    # 5.6 net-current runtime folder validation
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @(), @("macOS", "NetCore")),
    
    # 5.6 net-current with XAML trimming validation - desktop
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net10.0-desktop", "-p:UnoXamlResourcesTrimming=true", "-p:PublishTrimmed=true", "-r", "win-x64"), @("NetCore", "Publish")),
    
    # 5.6 net-current with XAML trimming validation - wasm
    @(3, "5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj", @("-f", "net10.0-browserwasm", "-p:UnoXamlResourcesTrimming=true", "-p:WasmShellILLinkerEnabled=true"), @("macOS", "NetCore", "Publish")),

    # Ensure that build can happen even if a RID is specified
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net9.0"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net9.0-browserwasm"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net9.0-ios"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net9.0-android"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net9.0-maccatalyst"), @("macOS", "NetCore")),
    @(4, "5.3/uno53AppWithLib/uno53AppWithLib/uno53AppWithLib.csproj", @("-f", "net9.0-desktop"), @("macOS", "NetCore")),

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
