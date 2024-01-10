@echo off
setlocal ENABLEEXTENSIONS

if '%Platform%' NEQ '' if '%CI%' NEQ '' goto errorci

if '%Platform%' NEQ '' if '%CI%' == '' goto question

:convert
echo Make sure that :
echo - Run on a clean repo (git clean -fdx) otherwise symbols will be incorrectly generated
echo - crosstargeting_override.props is not defininig UnoTargetFrameworkOverride
echo - This script is run from the build folder (not src/build)
pause
set myvar=%cd%
pushd ..\src\Uno.WinUIRevert
dotnet run %myvar%\..
popd

set UnoDisableNetCurrentMobile=true
set UnoDisableNetCurrent=true
msbuild Uno.UI.Build.csproj "/p:CombinedConfiguration=Release|AnyCPU;BUILD_BUILDNUMBER=test_test_8888" /m /t:RunAPISyncTool /v:m /bl

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
  goto convert
:end
endlocal
