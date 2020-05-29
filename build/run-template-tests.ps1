$ErrorActionPreference = 'Stop'

function Get-TemplateConfiguration(
    [bool]$uwp = $false,
    [bool]$android = $false,
    [bool]$iOS = $false,
    [bool]$macOS = $false,
    [bool]$wasm = $false,
    [bool]$wasmVsCode = $false)
{
    $uwpFlag = '-uwp'
    $androidFlag = '-android'
    $iOSFlag = '-ios'
    $macOSFlag = '-macos'
    $wasmFlag = '-wasm'
    $wasmVsCodeFlag = '--vscodeWasm'

    $a = If ($uwp)        { $uwpFlag }        Else { $uwpFlag        + '=false' }
    $b = If ($android)    { $androidFlag }    Else { $androidFlag    + '=false' }
    $c = If ($iOS)        { $iOSFlag }        Else { $iOSFlag        + '=false' }
    $d = If ($macOS)      { $macOSFlag }      Else { $macOSFlag      + '=false' }
    $e = If ($wasm)       { $wasmFlag }       Else { $wasmFlag       + '=false' }
    $f = If ($wasmVsCode) { $wasmVsCodeFlag } Else { $wasmVsCodeFlag + '=false' }

    @($a, $b, $c, $d, $e, $f)
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
    (Get-TemplateConfiguration -uwp 1),
    (Get-TemplateConfiguration -android 1),
    (Get-TemplateConfiguration -iOS 1),
    (Get-TemplateConfiguration -macOS 1),
    (Get-TemplateConfiguration -wasm 1)
)

$configurations =
@(
    @($templateConfigurations[0], $releaseX64),
    @($templateConfigurations[1], $release),
    @($templateConfigurations[2], $releaseIPhone),
    @($templateConfigurations[3], $releaseIPhoneSimulator),
    @($templateConfigurations[4], $release)
)

# Default
dotnet new unoapp -n UnoAppAll
& $msbuild $debug UnoAppAll\UnoAppAll.sln

# Heads - Release
for($i = 0; $i -lt $configurations.Length; $i++)
{
    dotnet new unoapp -n "UnoApp$i" $configurations[$i][0]
    & $msbuild $configurations[$i][1] "UnoApp$i\UnoApp$i.sln"
}

# VS Code
dotnet new unoapp -n UnoAppVsCode (Get-TemplateConfiguration -wasm 1 -wasmVsCode 1)
dotnet build -p:RestoreConfigFile=$env:NUGET_CI_CONFIG UnoAppVsCode\UnoAppVsCode.sln

# WinUI - Release
# dotnet new unoapp-winui -n UnoAppWinUI
# & $msbuild $release UnoAppWinUI\UnoAppWinUI.sln
