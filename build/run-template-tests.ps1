$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

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
dotnet build -p:RestoreConfigFile=$env:NUGET_CI_CONFIG UnoAppVsCode\UnoAppVsCode.sln
Assert-ExitCodeIsZero

# Namespace Tests
dotnet new unoapp -n MyApp.Uno
& $msbuild $debug MyApp.Uno\MyApp.Uno.sln
Assert-ExitCodeIsZero

dotnet new unoapp -n MyApp.Android (Get-TemplateConfiguration -android 1)
& $msbuild $debug MyApp.Android\MyApp.Android.sln
Assert-ExitCodeIsZero

dotnet new unolib-crossruntime -n MyCrossRuntimeLib
& $msbuild $debug /t:Pack MyCrossRuntimeLib\MyCrossRuntimeLib.sln
Assert-ExitCodeIsZero

# WinUI - Default
dotnet new unoapp-winui -n UnoAppWinUI
& $msbuild $debug UnoAppWinUI\UnoAppWinUI.sln
Assert-ExitCodeIsZero
