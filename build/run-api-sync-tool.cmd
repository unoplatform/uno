@echo off
echo Make sure that :
echo - This script runs on Windows and in a Developer Command Prompt for Visual Studio (2019 or 2022)
echo - crosstargeting_override.props is not defininig UnoTargetFrameworkOverride
echo - This script is run from the build folder (not src/build)
pause

msbuild Uno.UI.Build.csproj "/p:CombinedConfiguration=Release|AnyCPU;BUILD_BUILDNUMBER=test_test_8888" /m /t:RunAPISyncTool /v:m /bl
pause
