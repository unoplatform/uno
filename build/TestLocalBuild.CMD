@echo off
setlocal ENABLEEXTENSIONS
set VSLANG=1033
SET DOTNET_CLI_UI_LANGUAGE=en-US
chcp 437 > nul
if '%Platform%' NEQ '' if '%CI%' NEQ '' goto errorci

if '%Platform%' NEQ '' if '%CI%' == '' goto question
:build
dotnet-gitversion /version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
 echo "Install gitversion with command: 'dotnet tool install GitVersion.Tool -g'"
 exit /B 1
)

for /f %%i in ('dotnet-gitversion /showvariable SemVer') do set GitVersion_SemVer=%%i
for /f %%i in ('dotnet-gitversion /showvariable MajorMinorPatch') do set GITVERSION_MajorMinorPatch=%%i
for /f %%i in ('dotnet-gitversion /showvariable CommitsSinceVersionSource') do set GITVERSION_CommitsSinceVersionSource=%%i
for /f %%i in ('dotnet-gitversion /showvariable VersionSourceSha') do set GITVERSION_VersionSourceSha=%%i

echo "GitVersion_SemVer                   :%GitVersion_SemVer%"
echo "GITVERSION_MajorMinorPatch          :%GITVERSION_MajorMinorPatch%"
echo "GITVERSION_CommitsSinceVersionSource:%GITVERSION_CommitsSinceVersionSource%"
echo "GITVERSION_VersionSourceSha         :%GITVERSION_VersionSourceSha%"

msbuild uno.ui.build.csproj "/p:CombinedConfiguration=Release|AnyCPU;BUILD_BUILDNUMBER=test_test_8888" /m:16 /clp:PerformanceSummary;Summary /bl %*
pause
goto end
:errorci
  echo Your system has the Platform environment variable to %Platform%, which is known to break some msbuild projects.
  exit /B 1
:question
  echo Your system has the Platform environment variable to %Platform%, which is known to break some msbuild projects.
  CHOICE /C YFC /M "Continue? Press Y for for continue with Platform=%Platform% , F for force Platform to empty or C for Cancel."
  set result=%errorlevel%
  If /I '%result%' == '2' (set Platform=)
  If /I '%result%' == '3' (exit /B 1)
  goto build
:end
endlocal

