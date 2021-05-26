Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

function Get-TemplateConfiguration(
    [bool]$android = $false,
    [bool]$iOS = $false,
    [bool]$macOS = $false,
    [bool]$wasm = $false,
    [bool]$skiaGtk = $false,
    [bool]$skiaWpf = $false,
    [bool]$skiaTizen = $false,
    [bool]$wasmVsCode = $false)
{
    $androidFlag = '-android'
    $iOSFlag = '-ios'
    $macOSFlag = '-macos'
    $wasmFlag = '-wasm'
    $wasmVsCodeFlag = '--vscodeWasm'
    $skiaWpfFlag = '--skia-wpf'
    $skiaGtkFlag = '--skia-gtk'
    $skiaTizenFlag = '--skia-tizen'

    $a = If ($android)    { $androidFlag }    Else { $androidFlag    + '=false' }
    $b = If ($iOS)        { $iOSFlag }        Else { $iOSFlag        + '=false' }
    $c = If ($macOS)      { $macOSFlag }      Else { $macOSFlag      + '=false' }
    $d = If ($wasm)       { $wasmFlag }       Else { $wasmFlag       + '=false' }
    $e = If ($wasmVsCode) { $wasmVsCodeFlag } Else { $wasmVsCodeFlag + '=false' }
    $f = If ($skiaWpf)    { $skiaWpfFlag    } Else { $skiaWpfFlag    + '=false' }
    $g = If ($skiaGtk)    { $skiaGtkFlag    } Else { $skiaGtkFlag    + '=false' }
    $h = If ($skiaTizen)  { $skiaTizenFlag  } Else { $skiaTizenFlag  + '=false' }

    @($a, $b, $c, $d, $e, $f, $g, $h)
}

$msbuild = vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

$default = @('/ds', '/r', "/p:RestoreConfigFile=$env:NUGET_CI_CONFIG", '/p:PackageCertificateKeyFile=')

$debug = $default + '/p:Configuration=Debug'

$release = $default + '/p:AotAssemblies=false' + '/p:Configuration=Release'
$releaseX64 = $release + '/p:Platform=x64'
$releaseIPhone = $release + '/p:Platform=iPhone'
$releaseIPhoneSimulator = $release + '/p:Platform=iPhoneSimulator'

$templateConfigurations =
@(
    (Get-TemplateConfiguration),
    (Get-TemplateConfiguration -android 1),
    (Get-TemplateConfiguration -iOS 1),
    (Get-TemplateConfiguration -macOS 1),
    (Get-TemplateConfiguration -wasm 1),
    (Get-TemplateConfiguration -skiaGtk 1),
    (Get-TemplateConfiguration -skiaWpf 1),
    (Get-TemplateConfiguration -skiaTizen 1)
)

$configurations =
@(
    @($templateConfigurations[0], $releaseX64),
    @($templateConfigurations[1], $release),
    @($templateConfigurations[2], $releaseIPhone),
    @($templateConfigurations[3], $releaseIPhoneSimulator),
    @($templateConfigurations[4], $release),
    @($templateConfigurations[5], $release),
    @($templateConfigurations[6], $release),
    @($templateConfigurations[7], $release)
)

# Default
dotnet new unoapp -n UnoAppAll
& $msbuild $debug UnoAppAll\UnoAppAll.sln
Assert-ExitCodeIsZero

# Heads - Release
for($i = 0; $i -lt $configurations.Length; $i++)
{
    dotnet new unoapp -n "UnoApp$i" $configurations[$i][0]
    & $msbuild $configurations[$i][1] "UnoApp$i\UnoApp$i.sln"
    Assert-ExitCodeIsZero
}

# VS Code
dotnet new unoapp -n UnoAppVsCode (Get-TemplateConfiguration -wasm 1 -wasmVsCode 1)
dotnet build -p:RestoreConfigFile=$env:NUGET_CI_CONFIG UnoAppVsCode\UnoAppVsCode.Wasm\UnoAppVsCode.Wasm.csproj
Assert-ExitCodeIsZero

# Namespace Tests
dotnet new unoapp -n MyApp.Uno
& $msbuild $debug MyApp.Uno\MyApp.Uno.sln
Assert-ExitCodeIsZero

dotnet new unoapp -n MyApp.Android (Get-TemplateConfiguration -android 1)
& $msbuild $debug MyApp.Android\MyApp.Android.sln
Assert-ExitCodeIsZero

# Uno Library
dotnet new unolib -n MyUnoLib
& $msbuild $debug /t:Pack MyUnoLib\MyUnoLib.csproj
Assert-ExitCodeIsZero

# Uno Cross-Runtime Library
dotnet new unolib-crossruntime -n MyCrossRuntimeLib
& $msbuild $debug /t:Pack MyCrossRuntimeLib\MyCrossRuntimeLib.sln
Assert-ExitCodeIsZero

# WinUI - Default
dotnet new unoapp-winui -n UnoAppWinUI
& $msbuild $debug UnoAppWinUI\UnoAppWinUI.sln
Assert-ExitCodeIsZero

# UI Tests template
dotnet new unoapp-uitest -o UnoUITests01
& $msbuild $debug UnoUITests01\UnoUITests01.csproj
Assert-ExitCodeIsZero

# Prism template test
dotnet new unoapp-prism -o UnoUIPrism01
& $msbuild $debug UnoUIPrism01\UnoUIPrism01.sln
Assert-ExitCodeIsZero

# XF - Default
7z x build\assets\xfapp-uwp-4.8.0.1451.zip -oXFApp

pushd XFApp
dotnet new wasmxfhead
& $msbuild /ds /r /p:Configuration=Debug XFApp.Wasm\XFApp.Wasm.csproj
Assert-ExitCodeIsZero
popd
