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

export NUNIT_VERSION=3.11.1
mono nuget/nuget.exe install NUnit.ConsoleRunner -Version $NUNIT_VERSION

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

	# Note for test authors, add tests in the last group, notify devops
	# notify devops when the group gets too big.
	# See https://github.com/unoplatform/uno/issues/1955 for additional details

	if [ "$UITEST_AUTOMATED_GROUP" == '1' ];
	then
		export TEST_FILTERS=" \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests' or \
			namespace = 'SamplesApp.UITests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Input.VisualState_Tests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.FlyoutTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.DatePickerTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.WUXProgressRingTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ListViewTests'
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '2' ];
	then
		export TEST_FILTERS=" \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Media.Animation_Tests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ControlTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBlockTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml.FocusManagerDirectionTests' or \
			namespace = 'SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests'
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '3' ];
	then
		export TEST_FILTERS=" \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.PivotTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.CommandBarTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ComboBoxTests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Media_Animation' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.BorderTests' or \
			class = 'SamplesApp.UITests.Windows_UI_Xaml_Shapes.Basics_Shapes_Tests' or \
			namespace = 'SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollViewerTests'
		"
	fi
fi

export UNO_UITEST_PLATFORM=iOS
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

# Imported app bundle from artifacts is not executable
chmod -R +x $UNO_UITEST_IOSBUNDLE_PATH

# Move to the screenshot directory so that the output path is the proper one, as
# required by Xamarin.UITest
cd $UNO_UITEST_SCREENSHOT_PATH

export UNO_ORIGINAL_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml
export UNO_RERUN_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-failed-rerun.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/failed-tests.txt

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.$NUNIT_VERSION/tools/nunit3-console.exe \
	--result=$UNO_ORIGINAL_TEST_RESULTS \
	--timeout=120000 \
	--where "$TEST_FILTERS" \
	$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/Release/net47/SamplesApp.UITests.dll \
	|| true


pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST
popd

if [ -n "`cat $UNO_TESTS_FAILED_LIST`" ]; then

	# Rerun failed tests
	echo Retrying failed tests

	echo Terminating Simulator instance
	killall Simulator || echo "Simulator was not running"

	mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.$NUNIT_VERSION/tools/nunit3-console.exe \
		--result=$UNO_RERUN_TEST_RESULTS \
		--timeout=120000 \
		--testlist $UNO_TESTS_FAILED_LIST \
		$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/Release/net47/SamplesApp.UITests.dll \
		|| true
fi
