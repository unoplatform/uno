#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

echo "Listing iOS simulators"
xcrun simctl list devices --json

/Applications/Xcode.app/Contents/Developer/Applications/Simulator.app/Contents/MacOS/Simulator &

cd $BUILD_SOURCESDIRECTORY

msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/SamplesApp.iOS.csproj
msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj

cd $BUILD_SOURCESDIRECTORY/build

mono nuget/nuget.exe install NUnit.ConsoleRunner -Version 3.10.0

export UNO_UITEST_PLATFORM=iOS
export UNO_UITEST_IOSBUNDLE_PATH=$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/bin/iPhoneSimulator/Release/SampleApp.app
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/ios

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe \
	--inprocess \
	--agents=1 \
	--workers=1 \
	$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/Release/net47/SamplesApp.UITests.dll \
	> $BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/ios/nunit-log.txt 2>&1 \
	|| true
