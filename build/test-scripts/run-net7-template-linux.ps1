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
    [bool]$mobile = $false,
    [bool]$wasm = $false,
    [bool]$skiaGtk = $false,
    [bool]$skiaWpf = $false,
    [bool]$skiaLinuxFB = $false,
    [bool]$wasmVsCode = $false)
{
    $mobileFlag = '-mobile'
    $iOSFlag = '-ios'
    $macOSFlag = '-macos'
    $wasmFlag = '-wasm'
    $wasmVsCodeFlag = '--vscode'
    $skiaWpfFlag = '--skia-wpf'
    $skiaGtkFlag = '--skia-gtk'
    $skiaLinuxFBFlag = '--skia-linux-fb'

    $a = If ($mobile)      { $mobileFlag     } Else { $mobileFlag      + '=false' }
    $d = If ($wasm)        { $wasmFlag       } Else { $wasmFlag        + '=false' }
    $e = If ($wasmVsCode)  { $wasmVsCodeFlag } Else { $wasmVsCodeFlag  + '=false' }
    $f = If ($skiaWpf)     { $skiaWpfFlag    } Else { $skiaWpfFlag     + '=false' }
    $g = If ($skiaGtk)     { $skiaGtkFlag    } Else { $skiaGtkFlag     + '=false' }
    $h = If ($skiaLinuxFB) { $skiaLinuxFBFlag} Else { $skiaLinuxFBFlag + '=false' }

    @($a, $b, $c, $d, $e, $f, $g, $h)
}

$default = @('-v', 'detailed', "-p:RestoreConfigFile=$env:NUGET_CI_CONFIG")

$debug = $default + '-c' + 'Debug'
$release = $default + '-c' + 'Release'

# WinUI
$createParams=(Get-TemplateConfiguration -wasm 1 -wasmVsCode 1 -skiaGtk 1 -skiaLinuxFB 1)
dotnet new unoapp -n UnoAppWinUI --framework net7.0 $createParams

dotnet build $debug UnoAppWinUI/UnoAppWinUI.App/UnoAppWinUI.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUI/UnoAppWinUI.App/UnoAppWinUI.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoAppWinUI/UnoAppWinUI.App/UnoAppWinUI.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUI/UnoAppWinUI.App/UnoAppWinUI.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoAppWinUI/UnoAppWinUI.App/UnoAppWinUI.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUI/UnoAppWinUI.App/UnoAppWinUI.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero
