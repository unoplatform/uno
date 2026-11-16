# Builds Dawn's static library from source for Windows. Dawn's release covers x64 only, and against the shared C++
# runtime; this one links the runtime statically, so the DLL does not need the Visual C++ redistributable.
# Same configuration as Dawn's own release build (.github/workflows/dawn-ci.cmake), without samples, tests or tools.
# Usage: build-source-windows.ps1 -Source <dir to clone into> -Arch arm64 -Prefix <install dir>
# Then: link-windows.ps1 -Lib <Prefix>\lib\webgpu_dawn.lib -Arch arm64 ...
param(
	[Parameter(Mandatory)][string]$Source,
	[ValidateSet('x64', 'arm64')][string]$Arch = 'arm64',
	[Parameter(Mandatory)][string]$Prefix)
$ErrorActionPreference = 'Stop'

$version = Get-Content (Join-Path $PSScriptRoot 'version.json') | ConvertFrom-Json
if (-not (Test-Path (Join-Path $Source '.git'))) {
	git init $Source
	git -C $Source remote add origin https://dawn.googlesource.com/dawn
}
if ((git -C $Source rev-parse HEAD 2>$null) -ne $version.commit) {
	git -C $Source fetch --depth 1 origin $version.commit
	git -C $Source checkout --force FETCH_HEAD
}

$cmake = (Get-Command cmake -ErrorAction SilentlyContinue).Source
if (-not $cmake) {
	# Visual Studio carries its own CMake.
	$vs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.CMake.Project -property installationPath
	$cmake = Join-Path $vs 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
}
Set-Alias cmake $cmake

$build = Join-Path $Source "out\$Arch"
$platform = if ($Arch -eq 'arm64') { 'ARM64' } else { 'x64' }
cmake -S $Source -B $build -G 'Visual Studio 17 2022' -A $platform `
	-C (Join-Path $Source '.github\workflows\dawn-ci.cmake') `
	-DCMAKE_BUILD_TYPE=Release `
	-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded -DABSL_MSVC_STATIC_RUNTIME=ON `
	-DDAWN_BUILD_MONOLITHIC_LIBRARY=STATIC `
	-DDAWN_USE_BUILT_DXC=OFF `
	-DDAWN_BUILD_SAMPLES=OFF -DDAWN_BUILD_TESTS=OFF -DDAWN_USE_GLFW=OFF `
	-DTINT_BUILD_TESTS=OFF -DTINT_BUILD_CMD_TOOLS=OFF -DTINT_BUILD_IR_BINARY=OFF -DDAWN_BUILD_PROTOBUF=OFF
if ($LASTEXITCODE -ne 0) { throw "configure failed ($LASTEXITCODE)" }
cmake --build $build --config Release --target webgpu_dawn --parallel
if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }
cmake --install $build --config Release --prefix $Prefix
if ($LASTEXITCODE -ne 0) { throw "install failed ($LASTEXITCODE)" }
Write-Host "Installed Dawn to $Prefix"
