@ECHO OFF
SETLOCAL

SET UnoTargetFrameworkOverride=netstandard2.0
for /f "delims=" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -format value -property productPath') do set productPath=%%i
start "%productPath%" "filters\Uno.UI-Skia-only.slnf"
