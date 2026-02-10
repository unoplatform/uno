#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

# Ensure this script has no BOM even if it ever gets committed with one again
# (the BOM only breaks the shebang at exec time, but this is a safety net).
sed -i '' $'1s/^\xEF\xBB\xBF//' "$0"

if [ "$UITEST_SNAPSHOTS_ONLY" == 'true' ];
then
	export SCREENSHOTS_FOLDERNAME=ios-Snap

	# CommandBar disabled: https://github.com/unoplatform/uno/issues/1955
	# GridView and ListView are also disabled for instabilities
	# runGroup is used to parallelize the snapshots tests on multiple agents
	export TEST_FILTERS=" \
		FullyQualifiedName ~ SamplesApp.UITests.Snap \
		& TestCategory !~ automated:Uno.UI.Samples.Content.UITests.CommandBar \
		& TestCategory !~ automated:SamplesApp.Windows_UI_Xaml_Controls.ListView \
		& TestCategory !~ automated:GenericApp.Views.Content.UITests.GridView \
		& TestCategory !~ automated:Uno.UI.Samples.Content.UITests.GridView \
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
			| Namespace = SamplesApp.UITests.MessageDialogTests \
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '2' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Windows_UI_Xaml_Media.Animation_Tests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ControlTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBlockTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml.FocusManagerDirectionTests \
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
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollViewerTests \
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '4' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ListViewTests \
		"
	elif [ "$UITEST_AUTOMATED_GROUP" == '5' ];
	then
		export TEST_FILTERS=" \
			Namespace = SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.ItemsControl \
			| Namespace = SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests \
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

if [ -n "${UITEST_VARIANT-}" ]; then
	export SCREENSHOTS_FOLDERNAME="$SCREENSHOTS_FOLDERNAME-$UITEST_VARIANT"
fi

export LOG_FILEPATH=$BUILD_SOURCESDIRECTORY/ios-ui-tests-logs/$SCREENSHOTS_FOLDERNAME/_logs
export LOG_PREFIX=`date +"%Y%m%d%H%M%S"`

# Create the log directory early so that the artifacts publish task works property
mkdir -p $LOG_FILEPATH

export UNO_UITEST_PLATFORM=iOS
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME

export UNO_ORIGINAL_TEST_RESULTS_DIRECTORY=$BUILD_SOURCESDIRECTORY/build
export UNO_ORIGINAL_TEST_RESULTS=$UNO_ORIGINAL_TEST_RESULTS_DIRECTORY/TestResult-original.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-ios-$SCREENSHOTS_FOLDERNAME-${UITEST_SNAPSHOTS_GROUP=automated}-${UITEST_AUTOMATED_GROUP=automated}-${UITEST_RUNTIME_TEST_GROUP=automated}.txt
export UNO_TESTS_RUNTIMETESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-runtime-tests-ios-$SCREENSHOTS_FOLDERNAME-${UITEST_SNAPSHOTS_GROUP=automated}-${UITEST_AUTOMATED_GROUP=automated}-${UITEST_RUNTIME_TEST_GROUP=automated}.txt
export UNO_TESTS_RESPONSE_FILE=$BUILD_SOURCESDIRECTORY/build/nunit.response
export UNO_TESTS_LOCAL_TESTS_FILE=$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests
export UNO_UITEST_BENCHMARKS_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/benchmarks/ios-automated
export UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH=$BUILD_SOURCESDIRECTORY/build/RuntimeTestResults-ios-automated.xml

export UNO_UITEST_SIMULATOR_VERSION="com.apple.CoreSimulator.SimRuntime.iOS-17-5"
export UNO_UITEST_SIMULATOR_NAME="iPad Pro (12.9-inch) (6th generation)"

export UnoTargetFrameworkOverride="net9.0-ios18.0"

UITEST_IGNORE_RERUN_FILE="${UITEST_IGNORE_RERUN_FILE:=false}"

if [ -f "$UNO_TESTS_FAILED_LIST" ] && [ `cat "$UNO_TESTS_FAILED_LIST"` = "invalid-test-for-retry" ] && [ "$UITEST_IGNORE_RERUN_FILE" != "true" ]; then
	# The test results file only contains the re-run marker and no
	# other test to rerun. We can skip this run.
	echo "The file $UNO_TESTS_FAILED_LIST does not contain tests to re-run, skipping."
	exit 0
fi

echo "Current system date"
date

# Wait while ios runtime 16.1 is not having simulators. The install process may 
# take a few seconds and "simctl list devices" may not return devices.
while true; do
	export UITEST_IOSDEVICE_ID=`xcrun simctl list -j | jq -r --arg sim "$UNO_UITEST_SIMULATOR_VERSION" --arg name "$UNO_UITEST_SIMULATOR_NAME" '.devices[$sim] | .[] | select(.name==$name) | .udid'`
	export UITEST_IOSDEVICE_DATA_PATH=`xcrun simctl list -j | jq -r --arg sim "$UNO_UITEST_SIMULATOR_VERSION" --arg name "$UNO_UITEST_SIMULATOR_NAME" '.devices[$sim] | .[] | select(.name==$name) | .dataPath'`

	if [ -n "$UITEST_IOSDEVICE_ID" ]; then
		break
	fi

	echo "Waiting for the simulator to be available"
	sleep 5
done

export DEVICELIST_FILEPATH=$LOG_FILEPATH/DeviceList-$LOG_PREFIX.json
echo "Listing iOS simulators to $DEVICELIST_FILEPATH"
xcrun simctl list devices --json > $DEVICELIST_FILEPATH

# Check for the presence of idb, and install it if it's not present
# NOTE: fb-idb currently breaks under Python 3.14 (asyncio get_event_loop change),
# so we pin fb-idb to Python 3.12 to avoid "There is no current event loop in thread 'MainThread'".
# Historical context: prior installs referenced an App Center issue/workaround.
# https://github.com/microsoft/appcenter/issues/2605#issuecomment-1854414963
export PATH=$PATH:~/.local/bin

if ! command -v idb >/dev/null 2>&1
then
	echo "Installing idb (fb-idb + idb-companion) pinned to Python 3.12"

	# 1) Make sure we have a usable python3.12, but don't fail if Homebrew linking conflicts
	if ! command -v python3.12 >/dev/null 2>&1; then
		# Install, but ignore link-step failure; we'll use the keg path explicitly
		brew list --versions python@3.12 >/dev/null 2>&1 || brew install python@3.12 || true
	fi
	# Prefer an existing python3.12 on PATH; otherwise use the keg path
	PY312_BIN="$(command -v python3.12 || echo "$(brew --prefix)/opt/python@3.12/bin/python3.12")"
	export PIPX_DEFAULT_PYTHON="$PY312_BIN"
	echo "Using Python for pipx: $PIPX_DEFAULT_PYTHON"

	# 2) Install helpers
	brew list --versions pipx >/dev/null 2>&1 || brew install pipx
	brew tap facebook/fb >/dev/null 2>&1 || true
	brew list --versions idb-companion >/dev/null 2>&1 || brew install idb-companion

	# 3) Install fb-idb under Python 3.12
	pipx uninstall fb-idb >/dev/null 2>&1 || true
	pipx install --force fb-idb
else
	echo "Using idb from: $(command -v idb)"
fi

##
## Pre-install the application to avoid https://github.com/microsoft/appcenter/issues/2389
##
echo "Starting simulator: [$UITEST_IOSDEVICE_ID] ($UNO_UITEST_SIMULATOR_VERSION / $UNO_UITEST_SIMULATOR_NAME)"
xcrun simctl boot "$UITEST_IOSDEVICE_ID" || true

# echo "Install app on simulator: $UITEST_IOSDEVICE_ID"
# xcrun simctl install "$UITEST_IOSDEVICE_ID" "$UNO_UITEST_IOSBUNDLE_PATH" || true
idb install --udid "$UITEST_IOSDEVICE_ID" "$UNO_UITEST_IOSBUNDLE_PATH"

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
		export SIMCTL_CHILD_UITEST_RUNTIME_CURRENT_TEST_FILE=/tmp/RuntimeCurrentTest-$UITEST_RUNTIME_TEST_GROUP.txt

	# $UNO_TESTS_RUNTIMETESTS_FAILED_LIST file exists
	if [ -f "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST" ]; then

		# if the  only contains `invalid-test-for-retry`, exit the script
		if [ `cat "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST"` = "invalid-test-for-retry" ]; then
			echo "The file $UNO_TESTS_RUNTIMETESTS_FAILED_LIST does not contain tests to re-run, skipping."
			exit 0
		fi

		export SIMCTL_CHILD_UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_RUNTIMETESTS_FAILED_LIST | base64 -b 0`

		# echo the failed filter list, if not empty
		if [ -n "$SIMCTL_CHILD_UITEST_RUNTIME_TESTS_FILTER" ]; then
			echo "Tests to run: $SIMCTL_CHILD_UITEST_RUNTIME_TESTS_FILTER"
		fi
	fi

	xcrun simctl launch "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID"

	# get the process id for the app
	export APP_PID=`xcrun simctl spawn "$UITEST_IOSDEVICE_ID" launchctl list | grep "$SAMPLESAPP_BUNDLE_ID" | awk '{print $1}'`
	echo "App PID: $APP_PID"

	# Set the timeout in seconds 
	UITEST_TEST_TIMEOUT_AS_MINUTES=${UITEST_TEST_TIMEOUT:0:${#UITEST_TEST_TIMEOUT}-1}
	TIMEOUT=$(($UITEST_TEST_TIMEOUT_AS_MINUTES * 60))
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

	if ! [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
 		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is not available, waiting 2 seconds"
 		sleep 2
 	fi
  
	# if the file exists, show a message
	if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is available, the test run is complete."

		# Copy the results to the build directory
		cp -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" "$UNO_ORIGINAL_TEST_RESULTS"
		
		RUNTIME_CURRENT_TEST_LOCAL="$BUILD_SOURCESDIRECTORY/build/runtime-current-test-ios-$UITEST_RUNTIME_TEST_GROUP.txt"
		if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_CURRENT_TEST_FILE" ]; then
			cp -f "$SIMCTL_CHILD_UITEST_RUNTIME_CURRENT_TEST_FILE" "$RUNTIME_CURRENT_TEST_LOCAL"
			echo "Last runtime test heartbeat: $(cat "$RUNTIME_CURRENT_TEST_LOCAL")"
		else
			echo "No runtime test heartbeat file found."
		fi
	else
		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is not available, the test run has timed out."
		if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_CURRENT_TEST_FILE" ]; then
			echo "Last runtime test heartbeat: $(cat "$SIMCTL_CHILD_UITEST_RUNTIME_CURRENT_TEST_FILE")"
		else
			echo "No runtime test heartbeat file found."
		fi
	fi

else

	echo "Test Parameters:"
	echo "  Timeout=$UITEST_TEST_TIMEOUT"
	echo "  Test filters: $UNO_TESTS_FILTER"

	## Run tests
	dotnet run -c Release -- --results-directory $UNO_ORIGINAL_TEST_RESULTS_DIRECTORY --hangdump --hangdump-timeout 45m --hangdump-filename hang.dump --settings .runsettings --filter "$UNO_TESTS_FILTER" || true
fi

# export the simulator logs
export TMP_LOG_FILEPATH=/tmp/DeviceLog-$LOG_PREFIX.logarchive
export LOG_FILE_DIRECTORY=$LOG_FILEPATH/$UITEST_AUTOMATED_GROUP-${UITEST_RUNTIME_TEST_GROUP=automated}-`date +"%Y%m%d%H%M%S"`
export LOG_FILEPATH_FULL=$LOG_FILE_DIRECTORY/DeviceLog-`date +"%Y%m%d%H%M%S"`.txt

mkdir -p $LOG_FILE_DIRECTORY

cp -fv "$UNO_ORIGINAL_TEST_RESULTS" $LOG_FILEPATH/Test-Results-$LOG_PREFIX.xml || true

# Copy all the dotnet test dmp files to the log directory
find $AGENT_TEMPDIRECTORY -name "*.dmp" -exec cp -v {} $LOG_FILEPATH \;
find $UNO_TESTS_LOCAL_TESTS_FILE -name "*.dmp" -exec cp -v {} $LOG_FILEPATH \;

## Take a screenshot
xcrun simctl io "$UITEST_IOSDEVICE_ID" screenshot $LOG_FILEPATH/capture-$LOG_PREFIX.png

## Capture the device logs
xcrun simctl spawn booted log collect --output $TMP_LOG_FILEPATH

## Shutting down simulator to reclaim memory
echo "Shutting down simulator"
xcrun simctl shutdown "$UITEST_IOSDEVICE_ID" || true

echo "Dumping device logs to $LOG_FILEPATH_FULL"
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
fi

echo "Copying crash reports"
cp -R ~/Library/Logs/DiagnosticReports/* $LOG_FILE_DIRECTORY || true

pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $UNO_ORIGINAL_TEST_RESULTS

if [ $? -eq 0 ]; then
	dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST
fi

if [ "$UITEST_AUTOMATED_GROUP" == 'RuntimeTests' ];
then
	## Fail the build when no runtime test results could be read
	dotnet run fail-empty $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE

	if [ $? -eq 0 ]; then
		dotnet run list-failed $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE $UNO_TESTS_RUNTIMETESTS_FAILED_LIST
	fi
fi

popd
