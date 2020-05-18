@echo off
echo Make sure that crosstargeting_override.props is not defininig UnoTargetFrameworkOverride
pause
msbuild Uno.UI.Build.csproj "/p:CombinedConfiguration=Release|AnyCPU;BUILD_BUILDNUMBER=test_test_8888" /m /t:RunAPISyncTool /clp:PerformanceSummary;Summary /bl
pause
