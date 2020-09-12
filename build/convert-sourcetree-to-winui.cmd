@echo off
echo Make sure that :
echo - crosstargeting_override.props is not defininig UnoTargetFrameworkOverride
echo - This script is run from the build folder (not src/build)
pause
set myvar=%cd%
pushd ..\src\Uno.WinUIRevert
dotnet run %myvar%\..
popd
msbuild Uno.UI.Build.csproj "/p:CombinedConfiguration=Release|AnyCPU;BUILD_BUILDNUMBER=test_test_8888" /m /t:RunAPISyncTool /clp:PerformanceSummary;Summary /bl
pause
