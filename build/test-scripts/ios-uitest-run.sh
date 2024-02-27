#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

if [ "$UITEST_SNAPSHOTS_ONLY" == 'true' ];
then
	export SCREENSHOTS_FOLDERNAME=ios-Snap

	# CommandBar disabled: https://github.com/unoplatform/uno/issues/1955
	# runGroup is used to parallelize the snapshots tests on multiple agents
	export TEST_FILTERS=" \
		FullyQualifiedName ~ SamplesApp.UITests.Snap \
		& TestCategory !~ automated:Uno.UI.Samples.Content.UITests.CommandBar \
		& TestCategory ~ runGroup:$UITEST_SNAPSHOTS_GROUP \
	"
else
	export SCREENSHOTS_FOLDERNAME=ios

	# Note for test authors, add tests in the last group, notify devops
	# notify devops when the group gets too big.
	# See https://github.com/unoplatform/uno/issues/1955 for additional details

	if [ "$UITEST_AUTOMATED_GROUP" == '1' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests \
			| Namespace = SamplesApp.UITests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Input.VisualState_Tests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.FlyoutTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.DatePickerTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.WUXProgressRingTests \
			| FullyQualifiedName ~ SamplesApp.UITests.Windows_UI_Xaml.DragAndDropTests.DragDrop_ListViewReorder_Automated \
			| Namespace = SamplesApp.UITests.MessageDialogTests
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '2' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Windows_UI_Xaml_Media.Animation_Tests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ControlTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBlockTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml.FocusManagerDirectionTests \
			| Namespace = SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ItemsControl \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '3' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.PivotTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.CommandBarTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ComboBoxTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Media_Animation \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.BorderTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.MenuFlyoutTests \
			| FullyQualifiedName ~ SamplesApp.UITests.Windows_UI_Xaml_Shapes.Basics_Shapes_Tests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '4' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ListViewTests \
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == 'RuntimeTests' ];
	then
		export TEST_FILTERS="FullyQualifiedName = SamplesApp.UITests.Runtime.RuntimeTests"

	elif [ "$UITEST_AUTOMATED_GROUP" == 'Benchmarks' ];
	then
		export TEST_FILTERS="FullyQualifiedName ~ SamplesApp.UITests.Runtime.BenchmarkDotNetTests"

	elif [ "$UITEST_AUTOMATED_GROUP" == 'Local' ];
	then
		# Use this group to debug failing UI tests locally
		export TEST_FILTERS="FullyQualifiedName ~ SamplesApp.UITests.Runtime.BenchmarkDotNetTests"
	fi
fi

export UNO_UITEST_PLATFORM=iOS
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME

export UNO_ORIGINAL_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-ios-$SCREENSHOTS_FOLDERNAME-${UITEST_SNAPSHOTS_GROUP=automated}-${UITEST_AUTOMATED_GROUP=automated}-${UITEST_RUNTIME_TEST_GROUP=automated}.txt
export UNO_TESTS_RESPONSE_FILE=$BUILD_SOURCESDIRECTORY/build/nunit.response
export UNO_TESTS_LOCAL_TESTS_FILE=$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests
export UNO_UITEST_BENCHMARKS_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/benchmarks/ios-automated
export UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH=$BUILD_SOURCESDIRECTORY/build/RuntimeTestResults-ios-automated.xml

export UNO_UITEST_SIMULATOR_VERSION="com.apple.CoreSimulator.SimRuntime.iOS-16-1"
export UNO_UITEST_SIMULATOR_NAME="iPad Pro (12.9-inch) (5th generation)"

UITEST_IGNORE_RERUN_FILE="${UITEST_IGNORE_RERUN_FILE:=false}"

if [ -f "$UNO_TESTS_FAILED_LIST" ] && [ `cat "$UNO_TESTS_FAILED_LIST"` = "invalid-test-for-retry" ] && [ "$UITEST_IGNORE_RERUN_FILE" != "true" ]; then
	# The test results file only contains the re-run marker and no
	# other test to rerun. We can skip this run.
	echo "The file $UNO_TESTS_FAILED_LIST does not contain tests to re-run, skipping."
	exit 0
fi

echo "Current system date"
date

echo "Listing iOS simulators"
xcrun simctl list devices --json

##
## Pre-install the application to avoid https://github.com/microsoft/appcenter/issues/2389
##
export UITEST_IOSDEVICE_ID=`xcrun simctl list -j | jq -r --arg sim "$UNO_UITEST_SIMULATOR_VERSION" --arg name "$UNO_UITEST_SIMULATOR_NAME" '.devices[$sim] | .[] | select(.name==$name) | .udid'`
export UITEST_IOSDEVICE_DATA_PATH=`xcrun simctl list -j | jq -r --arg sim "$UNO_UITEST_SIMULATOR_VERSION" --arg name "$UNO_UITEST_SIMULATOR_NAME" '.devices[$sim] | .[] | select(.name==$name) | .dataPath'`

echo "Starting simulator: [$UITEST_IOSDEVICE_ID] ($UNO_UITEST_SIMULATOR_VERSION / $UNO_UITEST_SIMULATOR_NAME)"
xcrun simctl boot "$UITEST_IOSDEVICE_ID" || true

echo "Install app on simulator: $UITEST_IOSDEVICE_ID"
xcrun simctl install "$UITEST_IOSDEVICE_ID" "$UNO_UITEST_IOSBUNDLE_PATH" || true

echo "Installing idb"
# https://github.com/microsoft/appcenter/issues/2605#issuecomment-1854414963
brew tap facebook/fb
brew install idb-companion
pip3 install fb-idb

## Pre-build the transform tool to get early warnings
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
dotnet build
popd

cd $BUILD_SOURCESDIRECTORY/build

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

# Imported app bundle from artifacts is not executable
sudo chmod -R +x $UNO_UITEST_IOSBUNDLE_PATH

# Move to the screenshot directory so that the output path is the proper one, as
# required by Xamarin.UITest
cd $UNO_UITEST_SCREENSHOT_PATH

## Build the NUnit configuration file
if [ -f "$UNO_TESTS_FAILED_LIST" ] && [ "$UITEST_IGNORE_RERUN_FILE" != "true" ]; then
    UNO_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST`
else
    UNO_TESTS_FILTER=$TEST_FILTERS
fi

cd $UNO_TESTS_LOCAL_TESTS_FILE

echo "Starting tests in mode $UITEST_AUTOMATED_GROUP"

if [ "$UITEST_AUTOMATED_GROUP" == 'RuntimeTests' ];
then
	export SIMCTL_CHILD_UITEST_RUNTIME_TEST_GROUP=$UITEST_RUNTIME_TEST_GROUP
	export SIMCTL_CHILD_UITEST_RUNTIME_TEST_GROUP_COUNT=$UITEST_RUNTIME_TEST_GROUP_COUNT
	export SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE=/tmp/TestResult-`date +"%Y%m%d%H%M%S"`.xml

	xcrun simctl launch "$UITEST_IOSDEVICE_ID" "uno.platform.samplesdev"

	# get the process id for the app
	export APP_PID=`xcrun simctl spawn "$UITEST_IOSDEVICE_ID" launchctl list | grep "uno.platform.samplesdev" | awk '{print $1}'`
	echo "App PID: $APP_PID"

	# Set the timeout in seconds 
	TIMEOUT=$((30 * 60))
	INTERVAL=15
	END_TIME=$((SECONDS+TIMEOUT))

	echo "Waiting for $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE to be available..."

	# Loop until the file exists or the timeout is reached
	while [[ ! -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" && $SECONDS -lt $END_TIME ]]; do
		# echo "Waiting $INTERVAL seconds for test results to be written to $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE";
		sleep $INTERVAL

		# exit loop if the APP_PID is not running anymore
		if ! ps -p $APP_PID > /dev/null; then
			echo "The app is not running anymore"
			break
		fi
	done

	# if the file exists, show a message
	if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is available, the test run is complete."

		# Copy the results to the build directory
		cp -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" "$UNO_ORIGINAL_TEST_RESULTS"

	else
		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is not available, the test run has timed out."
	fi

else
	echo "Test Parameters:"
	echo "  Timeout=$UITEST_TEST_TIMEOUT"
	echo "  Test filters: $UNO_TESTS_FILTER"

	## Run tests
	dotnet test \
		-c Release \
		-l:"console;verbosity=normal" \
		--logger "nunit;LogFileName=$UNO_ORIGINAL_TEST_RESULTS" \
		--filter "$UNO_TESTS_FILTER" \
		--blame-hang-timeout $UITEST_TEST_TIMEOUT \
		-v m \
		|| true
fi

# export the simulator logs
export LOG_FILEPATH=$BUILD_SOURCESDIRECTORY/ios-ui-tests-logs/$SCREENSHOTS_FOLDERNAME/_logs
export LOG_PREFIX=`date +"%Y%m%d%H%M%S"`
export TMP_LOG_FILEPATH=/tmp/DeviceLog-$LOG_PREFIX.logarchive
export LOG_FILEPATH_FULL=$LOG_FILEPATH/DeviceLog-$UITEST_AUTOMATED_GROUP-${UITEST_RUNTIME_TEST_GROUP=automated}-`date +"%Y%m%d%H%M%S"`.txt

cp -fv "$UNO_ORIGINAL_TEST_RESULTS" $LOG_FILEPATH/Test-Results-$LOG_PREFIX.xml || true

mkdir -p $LOG_FILEPATH

## Take a screenshot
xcrun simctl io "$UITEST_IOSDEVICE_ID" screenshot $LOG_FILEPATH/capture-$LOG_PREFIX.png

## Capture the device logs
xcrun simctl spawn booted log collect --output $TMP_LOG_FILEPATH

echo "Dumping device logs"
log show --style syslog $TMP_LOG_FILEPATH > $LOG_FILEPATH_FULL

echo "Searching for failures in device logs"
if grep -Eq "mini-generic-sharing.c:\d+, condition \`oti' not met" $LOG_FILEPATH_FULL
then
	# The application may crash without known cause, add a marker so the job can be restarted in that case.
    echo "##vso[task.logissue type=error]UNOBLD001: mini-generic-sharing.c:XXX assertion reached (https://github.com/unoplatform/uno/issues/8167)"
fi

if grep -cq "Unhandled managed exception: Watchdog failed" $LOG_FILEPATH_FULL
then
	# The application UI thread stalled
    echo "##vso[task.logissue type=error]UNOBLD002: Unknown failure, UI Thread Watchdog failed"
fi

if [ ! -f "$UNO_ORIGINAL_TEST_RESULTS" ]; then
	echo "##vso[task.logissue type=error]UNOBLD003: ERROR: The test results file $UNO_ORIGINAL_TEST_RESULTS does not exist (did nunit crash ?)"
	return 1
fi

if ! iconv -f UTF-16 -t UTF-8 "$UNO_ORIGINAL_TEST_RESULTS" | grep -q "<test-case" ; then
    echo "No test results found in the file, this can indicate a filter error."
    exit 1
fi

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})
dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST
popd
