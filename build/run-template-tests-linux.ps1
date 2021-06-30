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
    [bool]$skiaLinuxFB = $false,
    [bool]$wasmVsCode = $false)
{
    $androidFlag = '-android'
    $iOSFlag = '-ios'
    $macOSFlag = '-macos'
    $wasmFlag = '-wasm'
    $wasmVsCodeFlag = '--vscodeWasm'
    $skiaWpfFlag = '--skia-wpf'
    $skiaGtkFlag = '--skia-gtk'
    $skiaLinuxFBFlag = '--skia-linux-fb'

    $a = If ($android)     { $androidFlag }    Else { $androidFlag     + '=false' }
    $b = If ($iOS)         { $iOSFlag }        Else { $iOSFlag         + '=false' }
    $c = If ($macOS)       { $macOSFlag }      Else { $macOSFlag       + '=false' }
    $d = If ($wasm)        { $wasmFlag }       Else { $wasmFlag        + '=false' }
    $e = If ($wasmVsCode)  { $wasmVsCodeFlag } Else { $wasmVsCodeFlag  + '=false' }
    $f = If ($skiaWpf)     { $skiaWpfFlag    } Else { $skiaWpfFlag     + '=false' }
    $g = If ($skiaGtk)     { $skiaGtkFlag    } Else { $skiaGtkFlag     + '=false' }
    $h = If ($skiaLinuxFB) { $skiaLinuxFB    } Else { $skiaLinuxFBFlag + '=false' }

    @($a, $b, $c, $d, $e, $f, $g, $h)
}

$default = @('-v', 'detailed', "-p:RestoreConfigFile=$env:NUGET_CI_CONFIG")

$debug = $default + '-c' + 'Debug'
$release = $default + '-c' + 'Release'

# UWP
dotnet new unoapp -n UnoApp ((Get-TemplateConfiguration -wasm 1 -wasmVsCode 1 -skiaGtk 1 -skiaLinuxFB 1) + '--skia-tizen=false')

dotnet build $debug UnoApp/UnoApp.Wasm/UnoApp.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoApp/UnoApp.Wasm/UnoApp.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoApp/UnoApp.Skia.Gtk/UnoApp.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoApp/UnoApp.Skia.Gtk/UnoApp.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoApp/UnoApp.Skia.Linux.FrameBuffer/UnoApp.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoApp/UnoApp.Skia.Linux.FrameBuffer/UnoApp.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero

# WinUI
dotnet new unoapp-winui -n UnoAppWinUI (Get-TemplateConfiguration -wasm 1 -wasmVsCode 1 -skiaGtk 1 -skiaLinuxFB 1)

dotnet build $debug UnoAppWinUI/UnoAppWinUI.Wasm/UnoAppWinUI.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUI/UnoAppWinUI.Wasm/UnoAppWinUI.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoAppWinUI/UnoAppWinUI.Skia.Gtk/UnoAppWinUI.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUI/UnoAppWinUI.Skia.Gtk/UnoAppWinUI.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoAppWinUI/UnoAppWinUI.Skia.Linux.FrameBuffer/UnoAppWinUI.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUI/UnoAppWinUI.Skia.Linux.FrameBuffer/UnoAppWinUI.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero
