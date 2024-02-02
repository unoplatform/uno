Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
    }
}

$default = @('-v', 'detailed', "-p:RestoreConfigFile=$env:NUGET_CI_CONFIG", '-p:EnableWindowsTargeting=true')

$debug = $default + '-c' + 'Debug'
$release = $default + '-c' + 'Release'

# WinUI
cd src/SolutionTemplate

dotnet build $debug UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Wasm/UnoAppWinUILinuxValidation.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Wasm/UnoAppWinUILinuxValidation.Wasm.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Gtk/UnoAppWinUILinuxValidation.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Gtk/UnoAppWinUILinuxValidation.Skia.Gtk.csproj
Assert-ExitCodeIsZero

dotnet build $debug UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero

dotnet build $release UnoAppWinUILinuxValidation/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer/UnoAppWinUILinuxValidation.Skia.Linux.FrameBuffer.csproj
Assert-ExitCodeIsZero
