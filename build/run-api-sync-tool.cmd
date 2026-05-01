@echo off
setlocal

echo This script runs in any Windows terminal with the .NET SDK on PATH.
echo Visual Studio / Developer Command Prompt is not required.
echo.
echo If src\crosstargeting_override.props exists, it will be moved aside for the
echo duration of the run and restored afterwards (including on failure).
pause

pushd "%~dp0"

set OVERRIDE_PATH=..\src\crosstargeting_override.props
set OVERRIDE_BACKUP=..\src\crosstargeting_override.props.syncgen-backup

set HAD_OVERRIDE=0
if exist "%OVERRIDE_PATH%" (
    if exist "%OVERRIDE_BACKUP%" (
        echo Existing backup file "%OVERRIDE_BACKUP%" found. Aborting to avoid clobbering it.
        goto :fail
    )
    echo Moving "%OVERRIDE_PATH%" aside to "%OVERRIDE_BACKUP%".
    move "%OVERRIDE_PATH%" "%OVERRIDE_BACKUP%" >nul || goto :fail
    set HAD_OVERRIDE=1
)

set SYNC_EXITCODE=0

dotnet restore filters\Uno.UI-top-projects-for-sync-gen.slnf -p:Configuration=Release -p:CI_Build=true -p:_IsCIBuild=true -p:SyncGeneratorRunning=true || goto :failed_step
dotnet build ..\src\Uno.WinAppSDKSyncGenerator\Uno.WinAppSDKSyncGenerator.csproj -c Release || goto :failed_step
dotnet build ..\src\Uno.WinAppSDKSyncGenerator.References\Uno.WinAppSDKSyncGenerator.References.csproj -c Release || goto :failed_step

..\src\Uno.WinAppSDKSyncGenerator\bin\Release\Uno.WinAppSDKSyncGenerator.exe sync
set SYNC_EXITCODE=%ERRORLEVEL%
goto :restore

:failed_step
set SYNC_EXITCODE=1

:restore
if "%HAD_OVERRIDE%"=="1" (
    echo Restoring "%OVERRIDE_PATH%" from backup.
    move /Y "%OVERRIDE_BACKUP%" "%OVERRIDE_PATH%" >nul
)

popd
pause
exit /b %SYNC_EXITCODE%

:fail
popd
pause
exit /b 1
