@ECHO OFF
SETLOCAL

SET UnoTargetFrameworkOverride=net461
for /f "delims=" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -format value -property productPath') do set productPath=%%i
start "%productPath%" "filters\Uno.UI-UnitTests-only.slnf"
