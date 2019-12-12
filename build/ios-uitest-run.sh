#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

echo "Listing iOS simulators"
xcrun simctl list devices --json

## Preemptively start the simulator
/Applications/Xcode.app/Contents/Developer/Applications/Simulator.app/Contents/MacOS/Simulator &

cd $BUILD_SOURCESDIRECTORY
msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj

cd $BUILD_SOURCESDIRECTORY/build

mono nuget/nuget.exe install NUnit.ConsoleRunner -Version 3.10.0

if [ "$UITEST_SNAPSHOTS_ONLY" == 'true' ];
then
	export SCREENSHOTS_FOLDERNAME=ios-Snap

	# CommandBar disabled: https://github.com/unoplatform/uno/issues/1955
	# runGroup is used to parallelize the snapshots tests on multiple agents
	export TEST_FILTERS=" \
		namespace == 'SamplesApp.UITests.Snap' \
		and Description !~ 'automated:Uno.UI.Samples.Content.UITests.CommandBar.*' \
		and Description =~ 'runGroup:$UITEST_SNAPSHOTS_GROUP' \
	"
else
	export SCREENSHOTS_FOLDERNAME=ios
	export TEST_FILTERS=" \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests' or \
		namespace = 'SamplesApp.UITests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Input.VisualState_Tests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.FlyoutTests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.DatePickerTests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ListViewTests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Media.Animation_Tests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ControlTests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBlockTests' or \
		namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests'
	"
fi

export UNO_UITEST_PLATFORM=iOS
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

# Imported app bundle from artifacts is not executable
chmod -R +x $UNO_UITEST_IOSBUNDLE_PATH

# Move to the screenshot directory so that the output path is the proper one, as
# required by Xamarin.UITest
cd $UNO_UITEST_SCREENSHOT_PATH

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe \
	--inprocess \
	--agents=1 \
	--workers=1 \
	--result=$BUILD_SOURCESDIRECTORY/build/TestResult.xml \
	--where "$TEST_FILTERS" \
	$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/Release/net47/SamplesApp.UITests.dll \
	|| true
