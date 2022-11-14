@echo off
setlocal ENABLEEXTENSIONS
set VSLANG=1033
SET DOTNET_CLI_UI_LANGUAGE = en-US
chcp 437

if '%Platform%' NEQ '' if '%CI%' NEQ '' goto errorci

if '%Platform%' NEQ '' if '%CI%' == '' goto question

:build
msbuild uno.ui.build.csproj "/p:CombinedConfiguration=Release|AnyCPU;BUILD_BUILDNUMBER=test_test_8888;GitVersion_FullSemVer=1.0.0.0;GITVERSION_MajorMinorPatch=1.0.0;GITVERSION_CommitsSinceVersionSource=1" /m:16 /clp:PerformanceSummary;Summary /bl
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


