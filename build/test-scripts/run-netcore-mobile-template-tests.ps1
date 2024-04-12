Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

$default = @('/ds', '/v:m', '/p:UseDotNetNativeToolchain=false', '/p:PackageCertificateKeyFile=')

if ($IsWindows) 
{
    $msbuild = vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
}

$debug = $default + '/p:Configuration=Debug' + '/r'
$release = $default + '/p:Configuration=Release' + '/r'

## Configurations are split to work around UWP not building with .NET new
$dotnetBuildConfigurations =
@(
    @("Mobile", "-f:net7.0-android", ""), # workaround for https://github.com/xamarin/xamarin-android/issues/7473
    @("Mobile", "-f:net7.0-ios", ""),
    @("Mobile", "-f:net7.0-maccatalyst", ""),
    # @("Mobile", "-f:net7.0-macos", ""), # workaround for https://github.com/xamarin/xamarin-macios/issues/16401
    @("Wasm", "", ""),
    @("Skia.Gtk", "", ""),
    @("Skia.Linux.FrameBuffer", "", "")
)

if ($IsWindows) 
{
    $dotnetBuildConfigurations = $dotnetBuildConfigurations 
        + @("Skia.WPF", "", "");
}

cd src/SolutionTemplate

# Debug Config
pushd UnoAppAll

for($i = 0; $i -lt $dotnetBuildConfigurations.Length; $i++)
{
    $platform=$dotnetBuildConfigurations[$i][0];
    & dotnet build -c Debug $default $dotnetBuildConfigurations[$i][1] $dotnetBuildConfigurations[$i][2] "UnoAppAll.$platform\UnoAppAll.$platform.csproj"
    Assert-ExitCodeIsZero
}

if ($IsWindows) 
{
    & $msbuild $debug "UnoAppAll.UWP\UnoAppAll.UWP.csproj"
    Assert-ExitCodeIsZero
}

for($i = 0; $i -lt $dotnetBuildConfigurations.Length; $i++)
{
    $platform=$dotnetBuildConfigurations[$i][0];
    & dotnet build -c Release $default $dotnetBuildConfigurations[$i][1] $dotnetBuildConfigurations[$i][2] "UnoAppAll.$platform\UnoAppAll.$platform.csproj"
    Assert-ExitCodeIsZero
}

if ($IsWindows) 
{
    & $msbuild $debug "UnoAppAll.UWP\UnoAppAll.UWP.csproj"
    Assert-ExitCodeIsZero
}

popd

$dotnetBuildNet6Configurations =
@(
    @("Mobile", "-f:net7.0-android", ""),
    @("Mobile", "-f:net7.0-ios", ""),
    @("Mobile", "-f:net7.0-maccatalyst", ""),
    # @("Mobile", "-f:net6.0-macos", ""),  # workaround for https://github.com/xamarin/xamarin-macios/issues/16401
    @("Wasm", "", ""),
    @("Server", "", ""),
    @("Skia.Gtk", "", ""),
    @("Skia.Linux.FrameBuffer", "", "")
)

if ($IsWindows) 
{
    $dotnetBuildNet6Configurations = $dotnetBuildNet6Configurations 
        + @("Skia.WPF", "", "");
}

# WinUI - Default
pushd UnoAppWinUI
for($i = 0; $i -lt $dotnetBuildNet6Configurations.Length; $i++)
{
    $platform=$dotnetBuildNet6Configurations[$i][0];
    & dotnet build -c Debug $default $dotnetBuildNet6Configurations[$i][1] $dotnetBuildNet6Configurations[$i][2] "UnoAppWinUI.$platform\UnoAppWinUI.$platform.csproj"
    Assert-ExitCodeIsZero
}

# Server project build (merge with above loop when .App folder is removed)
& dotnet build -c Debug $default "UnoAppWinUI.Server\UnoAppWinUI.Server.csproj"

if ($IsWindows) 
{
    # Build with msbuild because of https://github.com/microsoft/WindowsAppSDK/issues/1652
    # force targetframeworks until we can get WinAppSDK to build with `dotnet build`
    & $msbuild $debug "/p:Platform=x86" "/p:TargetFrameworks=net7.0-windows10.0.19041;TargetFramework=net7.0-windows10.0.19041" "UnoAppWinUI.Windows\UnoAppWinUI.Windows.csproj"
    Assert-ExitCodeIsZero
}

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
    dotnet build MyAppXamlTrim\MyAppXamlTrim.Wasm\MyAppXamlTrim.Wasm.csproj /p:UnoXamlResourcesTrimming=true /p:WasmShellILLinkerEnabled=true
    Assert-ExitCodeIsZero

    dotnet run --project ..\Uno.ResourceTrimmingValidator\Uno.ResourceTrimmingValidator.csproj -- -a (Get-ChildItem MyAppXamlTrim.Wasm.clr -Recurse).FullName -r Strings.en.Resources.upri -x Strings.fr.Resources.upri
    Assert-ExitCodeIsZero

    dotnet run --project ..\Uno.ResourceTrimmingValidator\Uno.ResourceTrimmingValidator.csproj -- -a (Get-ChildItem Uno.UI.clr -Recurse).FullName -r Resources.Strings.en.Resources.upri -r UI.Xaml.DragDrop.Strings.en-US.Resources.upri -x Resources.Strings.cs-CZ.Resources.upri
    Assert-ExitCodeIsZero

    # Uno Library
    # Mobile is removed for now, until we can get net7 supported by msbuild/VS 17.4
    & $msbuild $debug /t:pack MyUnoLib\MyUnoLib.csproj "/p:TargetFrameworks=`"net7.0-windows10.0.19041;net7.0`""
    Assert-ExitCodeIsZero

    # Uno Cross-Runtime Library
    & $msbuild $debug /t:Pack MyCrossRuntimeLib\MyCrossRuntimeLib.sln
    Assert-ExitCodeIsZero

    #
    # Uno Library with assets, Validate assets count
    #
    # Mobile is removed for now, until we can get net7 supported by msbuild/VS 17.4
    & $msbuild $debug /t:pack /p:IncludeContentInPack=false MyUnoLib2\MyUnoLib2.csproj -bl "/p:TargetFrameworks=`"net7.0-windows10.0.19041;net7.0`""
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


## Tests Per versions of uno
if ($IsWindows)
{
    $default = @('-v:m', '-p:EnableWindowsTargeting=true')
}
else
{
    $default = @('-v:m')
}

$debug = $default + '-p:Configuration=Debug'
$release = $default + '-p:Configuration=Release'

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
    #
    # 5.1 Blank
    #
    @("5.1/uno51blank/uno51blank.Mobile/uno51blank.Mobile.csproj", "", $true, $true),
    @("5.1/uno51blank/uno51blank.Skia.Gtk/uno51blank.Skia.Gtk.csproj", "", $true, $true),
    @("5.1/uno51blank/uno51blank.Skia.Linux.FrameBuffer/uno51blank.Skia.Linux.FrameBuffer.csproj", "", $true, $true),
    @("5.1/uno51blank/uno51blank.Skia.Wpf/uno51blank.Skia.Wpf.csproj", "", $true, $false),
    @("5.1/uno51blank/uno51blank.Wasm/uno51blank.Wasm.csproj", "", $true, $true),
    @("5.1/uno51blank/uno51blank.Windows/uno51blank.Windows.csproj", "", $false, $false),

    #
    # 5.1 Recommended
    #
    @("5.1/uno51recommended/uno51recommended.Mobile/uno51recommended.Mobile.csproj", "", $true, $true),
    @("5.1/uno51recommended/uno51recommended.Windows/uno51recommended.Windows.csproj", "", $false, $false),
    @("5.1/uno51recommended/uno51recommended.Skia.Gtk/uno51recommended.Skia.Gtk.csproj", "", $true, $true),
    @("5.1/uno51recommended/uno51recommended.Skia.Linux.FrameBuffer/uno51recommended.Skia.Linux.FrameBuffer.csproj", "", $true, $true),
    @("5.1/uno51recommended/uno51recommended.Skia.Wpf/uno51recommended.Skia.Wpf.csproj", "", $true, $false),
    @("5.1/uno51recommended/uno51recommended.Wasm/uno51recommended.Wasm.csproj", "", $true, $true),
    @("5.1/uno51recommended/uno51recommended.Server/uno51recommended.Server.csproj", "", $true, $true),
    @("5.1/uno51recommended/uno51recommended.Tests/uno51recommended.Tests.csproj", "", $true, $true),
    @("5.1/uno51recommended/uno51recommended.UITests/uno51recommended.UITests.csproj", "", $true, $true),

    #
    # 5.2 Blank
    #
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-f", "net8.0"), $true, $true),
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-f", "net8.0-browserwasm"), $true, $true),
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-f", "net8.0-ios"), $true, $true),
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-f", "net8.0-android"), $true, $true),
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-f", "net8.0-maccatalyst"), $true, $true),
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-f", "net8.0-desktop"), $true, $true),

    # Default mode for the template is WindowsAppSDKSelfContained=true, which requires specifying a target platform.
    @("5.2/uno52blank/uno52blank/uno52blank.csproj", @("-p:Platform=x86" , "-p:TargetFramework=net8.0-windows10.0.19041"), $false, $false)

    #
    # 5.2 Uno Lib
    #
    @("5.2/uno52Lib/uno52Lib.csproj", @("-f", "net8.0"), $true, $true),
    @("5.2/uno52Lib/uno52Lib.csproj", @("-f", "net8.0-browserwasm"), $true, $true),
    @("5.2/uno52Lib/uno52Lib.csproj", @("-f", "net8.0-ios"), $true, $true,
    @("5.2/uno52Lib/uno52Lib.csproj", @("-f", "net8.0-android"), $true, $true),
    @("5.2/uno52Lib/uno52Lib.csproj", @("-f", "net8.0-maccatalyst"), $true, $true),
    @("5.2/uno52Lib/uno52Lib.csproj", @("-f", "net8.0-desktop"), $true, $true),

    # Default mode for the template is WindowsAppSDKSelfContained=true, which requires specifying a target platform.
    @("5.2/uno52Lib/uno52Lib.csproj", @("-p:Platform=x86" , "-p:TargetFramework=net8.0-windows10.0.19041"), $false, $true),

    #
    # 5.2 Uno SingleProject Lib
    #
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-f", "net8.0"), $true, $true),
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-f", "net8.0-browserwasm"), $true, $true),
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-f", "net8.0-ios"), $true, $true),
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-f", "net8.0-android"), $true, $true),
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-f", "net8.0-maccatalyst"), $true, $true),
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-f", "net8.0-desktop"), $true, $true),

    # Default mode for the template is WindowsAppSDKSelfContained=true, which requires specifying a target platform.
    @("5.2/uno52SingleProjectLib/uno52SingleProjectLib.csproj", @("-p:Platform=x86" , "-p:TargetFramework=net8.0-windows10.0.19041"), $false, $false)

    ## Note for contributors
    ##
    ## When adding new template versions, create them in a separate version named folder
    ## using all the specific features that can be impacted by the use of the Uno.SDK
);

for($i = 0; $i -lt $projects.Length; $i++)
{
    $projectPath=$projects[$i][0];
    $projectOptions=$projects[$i][1];
    $buildWithNetCore=$projects[$i][2];
    $runOnMacOS=$projects[$i][3];

    if ($IsMacOS -and -not $runOnMacOS)
    {
        Write-Host "Skipping on macOS: $projectPath with $projectOptions"
        continue
    }

    if ($buildWithNetCore)
    {
        Write-Host "NetCore Building Debug $projectPath with $projectOptions"
        dotnet build $debug "$projectPath" $projectOptions
        Assert-ExitCodeIsZero

        Write-Host "NetCore Building Release $projectPath with $projectOptions"
        dotnet build $release "$projectPath" $projectOptions
        Assert-ExitCodeIsZero
    }
    else
    {
        if ($IsWindows) 
        {
            Write-Host "MSBuild Building Debug $projectPath with $projectOptions"
            & $msbuild $debug /r "$projectPath" $projectOptions
            Assert-ExitCodeIsZero

            Write-Host "MSBuild Building Release $projectPath with $projectOptions"
            & $msbuild $release /r "$projectPath" $projectOptions
            Assert-ExitCodeIsZero
        }
    }
}
