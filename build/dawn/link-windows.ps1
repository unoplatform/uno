# Links Dawn's static library into webgpu.dll, exporting the webgpu.h API. Expects a library built against the static
# C++ runtime (build-source-windows.ps1), so the DLL depends on nothing beyond Windows itself.
# Usage: link-windows.ps1 -Lib <webgpu_dawn.lib> -Exports <dir with webgpu_dawn.def> -Arch x64|arm64 -Out <dir>
param(
	[Parameter(Mandatory)][string]$Lib,
	[Parameter(Mandatory)][string]$Exports,
	[ValidateSet('x64', 'arm64')][string]$Arch = 'x64',
	[Parameter(Mandatory)][string]$Out)
$ErrorActionPreference = 'Stop'

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$component = if ($Arch -eq 'arm64') { 'Microsoft.VisualStudio.Component.VC.Tools.ARM64' } else { 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64' }
$vs = & $vswhere -latest -products * -requires $component -property installationPath
if (-not $vs) { throw "No Visual Studio with $component" }
$vcvars = Join-Path $vs 'VC\Auxiliary\Build\vcvarsall.bat'
$target = if ($Arch -eq 'arm64') { 'x64_arm64' } else { 'x64' }

New-Item -ItemType Directory -Force -Path $Out | Out-Null
$obj = Join-Path $Out 'stl_shims.obj'
$shims = Join-Path $PSScriptRoot 'stl_shims.cpp'
$def = Join-Path $Exports 'webgpu_dawn.def'
$dll = Join-Path $Out 'webgpu.dll'
# The libraries Dawn's D3D12, D3D11 and Vulkan backends link against; DXGI and D3D are loaded at run time.
$sys = 'user32.lib onecore_apiset.lib dxguid.lib advapi32.lib dbghelp.lib'
$cmd = "`"$vcvars`" $target >nul && cl /nologo /c /O2 /MT /Fo`"$obj`" `"$shims`" && link /nologo /DLL /OUT:`"$dll`" /DEF:`"$def`" /OPT:REF /OPT:ICF `"$obj`" /WHOLEARCHIVE:`"$Lib`" $sys"
cmd /c $cmd
if ($LASTEXITCODE -ne 0) { throw "link failed ($LASTEXITCODE)" }
Remove-Item (Join-Path $Out 'webgpu.exp'), (Join-Path $Out 'webgpu.lib'), $obj -ErrorAction SilentlyContinue
Write-Host "Linked $dll"
