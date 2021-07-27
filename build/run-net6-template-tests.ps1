Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

$default = @('/ds', "/p:RestoreConfigFile=$env:NUGET_CI_CONFIG", '/p:PackageCertificateKeyFile=')
$msbuild = vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

$debug = $default + '/p:Configuration=Debug' + '/r'
$release = $default + '/p:Configuration=Release' + '/r'

## Configurations are split to work around UWP not building with .NET new
$dotnetBuildConfigurations =
@(
    @("Mobile", "-f:net6.0-android"),
    @("Mobile", "-f:net6.0-ios"),
    @("Mobile", "-f:net6.0-maccatalyst"),
    @("Mobile", "-f:net6.0-macos"),
    @("Wasm", ""),
    @("Skia.Gtk", ""),
    @("Skia.Linux.FrameBuffer", ""),
    @("Skia.WPF.Host", "")
)

# Debug Config
dotnet new unoapp-net6 -n UnoAppAll

pushd UnoAppAll

for($i = 0; $i -lt $dotnetBuildConfigurations.Length; $i++)
{
    $platform=$dotnetBuildConfigurations[$i][0];
    & dotnet build -c Debug $default $dotnetBuildConfigurations[$i][1] "UnoAppAll.$platform\UnoAppAll.$platform.csproj"
    Assert-ExitCodeIsZero
}

& $msbuild $debug "UnoAppAll.UWP\UnoAppAll.UWP.csproj"
Assert-ExitCodeIsZero

for($i = 0; $i -lt $dotnetBuildConfigurations.Length; $i++)
{
    $platform=$dotnetBuildConfigurations[$i][0];
    & dotnet build -c Release $default $dotnetBuildConfigurations[$i][1] "UnoAppAll.$platform\UnoAppAll.$platform.csproj"
    Assert-ExitCodeIsZero
}

& $msbuild $debug "UnoAppAll.UWP\UnoAppAll.UWP.csproj"
Assert-ExitCodeIsZero

popd

$dotnetBuildNet6Configurations =
@(
    @("Mobile", "-f:net6.0-android"),
    @("Mobile", "-f:net6.0-ios"),
    @("Mobile", "-f:net6.0-maccatalyst"),
    @("Mobile", "-f:net6.0-macos"),
    @("Wasm", ""),
    @("Skia.Gtk", ""),
    @("Skia.Linux.FrameBuffer", ""),
    # @("Windows.Desktop", ""), # Disabled because of https://github.com/microsoft/microsoft-ui-xaml/issues/5143
    @("Skia.WPF.Host", "")
)

# WinUI - Default
dotnet new unoapp-winui-net6 -n UnoAppWinUI

pushd UnoAppWinUI
for($i = 0; $i -lt $dotnetBuildNet6Configurations.Length; $i++)
{
    $platform=$dotnetBuildNet6Configurations[$i][0];
    & dotnet build -c Debug $default $dotnetBuildNet6Configurations[$i][1] "UnoAppWinUI.$platform\UnoAppWinUI.$platform.csproj"
    Assert-ExitCodeIsZero
}

popd

# XAML Trimming build smoke test
dotnet new unoapp-net6 -n MyAppXamlTrim
& dotnet build -c Debug MyAppXamlTrim\MyAppXamlTrim.Wasm\MyAppXamlTrim.Wasm.csproj /p:UnoXamlResourcesTrimming=true
Assert-ExitCodeIsZero
