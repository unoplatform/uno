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

NUNIT_TOOL_DIR="$BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"
run_nunit_tool_ios() { (cd "$NUNIT_TOOL_DIR" && dotnet run --no-build "$@"); }

# Maximum failures to trigger an in-job retry; overridable via env var.
FLAKE_RETRY_MAX_FAILURES=${FLAKE_RETRY_MAX_FAILURES:-20}

# Maximum seconds between heartbeat emissions before the watchdog kills the app.
HEARTBEAT_TIMEOUT_S=${HEARTBEAT_TIMEOUT_S:-300}

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

	# Stream simulator device logs in background; filter for heartbeat markers.
	# Console.WriteLine from .NET iOS is routed through NSLog and appears in the simulator syslog.
	IOS_HEARTBEAT_LOG=$(mktemp /tmp/uno-rt-heartbeat.XXXXXX)
	xcrun simctl spawn "$UITEST_IOSDEVICE_ID" log stream \
		--predicate 'eventMessage CONTAINS "UNO-RT-HEARTBEAT"' \
		>> "$IOS_HEARTBEAT_LOG" &
	IOS_LOG_STREAM_PID=$!

	echo "Waiting for $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE to be available..."

	# Heartbeat watchdog state (count-based, mirrors Android approach).
	# IOS_HB_COUNT tracks lines in the stream file; if count stalls the test is hung.
	# IOS_LAST_HB_S tracks when count last advanced, for age calculation.
	IOS_PREV_HB_COUNT=0
	IOS_LAST_HB_S=$SECONDS
	IOS_HUNG_TEST_NAME=""
	IOS_HUNG_HB_AGE=0

	# Loop until the file exists or the timeout is reached
	while [[ ! -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" && $SECONDS -lt $END_TIME ]]; do
		sleep $INTERVAL

		# Count heartbeat lines captured so far by the background stream.
		IOS_HB_COUNT=$(grep -c 'UNO-RT-HEARTBEAT' "$IOS_HEARTBEAT_LOG" 2>/dev/null || echo 0)
		if [ "$IOS_HB_COUNT" -gt "$IOS_PREV_HB_COUNT" ]; then
			IOS_PREV_HB_COUNT=$IOS_HB_COUNT
			IOS_LAST_HB_S=$SECONDS
		fi
		IOS_HB_AGE=$(( SECONDS - IOS_LAST_HB_S ))

		# Watchdog: heartbeats have started but stopped advancing.
		if [ "$IOS_PREV_HB_COUNT" -gt 0 ] && [ "$IOS_HB_AGE" -gt "$HEARTBEAT_TIMEOUT_S" ]; then
			echo "##[error]No heartbeat for ${IOS_HB_AGE}s — test appears hung. Last heartbeats:"
			tail -3 "$IOS_HEARTBEAT_LOG"
			IOS_HUNG_TEST_NAME=$(grep 'STARTING' "$IOS_HEARTBEAT_LOG" | tail -1 | sed 's/.*STARTING //')
			IOS_HUNG_HB_AGE=$IOS_HB_AGE
			[ -n "$IOS_HUNG_TEST_NAME" ] && echo "##[error]Hung test identified: $IOS_HUNG_TEST_NAME"
			kill $IOS_LOG_STREAM_PID 2>/dev/null || true
			rm -f "$IOS_HEARTBEAT_LOG" || true
			xcrun simctl terminate "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID" || true
			break
		fi

		# Fallback: no heartbeats at all after 2× the timeout — app may be stuck at startup
		# or the log stream is not capturing heartbeats. Kill to avoid hour-long hangs.
		if [ "$IOS_PREV_HB_COUNT" -eq 0 ] && [ "$IOS_HB_AGE" -gt $(( HEARTBEAT_TIMEOUT_S * 2 )) ]; then
			echo "##[error]No heartbeats received for ${IOS_HB_AGE}s — app stuck at startup or log stream not working. Terminating."
			kill $IOS_LOG_STREAM_PID 2>/dev/null || true
			rm -f "$IOS_HEARTBEAT_LOG" || true
			xcrun simctl terminate "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID" || true
			break
		fi

		# exit loop if the APP_PID is not running anymore
		if ! ps -p $APP_PID > /dev/null; then
			echo "The app is not running anymore"
			break
		fi
	done

	kill $IOS_LOG_STREAM_PID 2>/dev/null || true
	rm -f "$IOS_HEARTBEAT_LOG" || true

	if ! [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
		sleep 2
	fi

	# Helper: wait for a simulator run and apply the heartbeat watchdog.
	# $1 = result file path, $2 = app pid, $3 = timeout end ($SECONDS), $4 = heartbeat log path, $5 = log stream PID
	ios_wait_for_result() {
		local result_file="$1" app_pid="$2" end_time="$3" hb_log="$4" stream_pid="$5"
		local prev_hb=0 last_hb_s=$SECONDS

		while [[ ! -f "$result_file" && $SECONDS -lt $end_time ]]; do
			sleep $INTERVAL
			local hb_count
			hb_count=$(grep -c 'UNO-RT-HEARTBEAT' "$hb_log" 2>/dev/null || echo 0)
			if [ "$hb_count" -gt "$prev_hb" ]; then prev_hb=$hb_count; last_hb_s=$SECONDS; fi
			local age=$(( SECONDS - last_hb_s ))
			if [ "$prev_hb" -gt 0 ] && [ "$age" -gt "$HEARTBEAT_TIMEOUT_S" ]; then
				echo "##[error]No heartbeat for ${age}s in retry run — test appears hung."
				tail -3 "$hb_log"
				kill "$stream_pid" 2>/dev/null || true
				rm -f "$hb_log" || true
				xcrun simctl terminate "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID" || true
				break
			fi
			if [ "$prev_hb" -eq 0 ] && [ "$age" -gt $(( HEARTBEAT_TIMEOUT_S * 2 )) ]; then
				echo "##[error]No heartbeats in retry for ${age}s — terminating."
				kill "$stream_pid" 2>/dev/null || true
				rm -f "$hb_log" || true
				xcrun simctl terminate "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID" || true
				break
			fi
			if ! ps -p "$app_pid" > /dev/null; then echo "The app is not running anymore (retry run)"; break; fi
		done
		kill "$stream_pid" 2>/dev/null || true
		rm -f "$hb_log" || true
	}

	if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is available, the test run is complete."

		# Copy the results to the build directory
		cp -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" "$UNO_ORIGINAL_TEST_RESULTS"

		# In-job retry: if a small number of tests failed, rerun only those to catch flakes.
		IOS_FAILED_COUNT=$(run_nunit_tool_ios count-failed "$UNO_ORIGINAL_TEST_RESULTS" | tail -1)

		if [ "$IOS_FAILED_COUNT" -gt 0 ] && [ "$IOS_FAILED_COUNT" -le "$FLAKE_RETRY_MAX_FAILURES" ]; then
			echo "##[warning]$IOS_FAILED_COUNT test(s) failed — retrying in-job to filter flakes..."

			mkdir -p $(dirname ${UNO_TESTS_RUNTIMETESTS_FAILED_LIST})
			run_nunit_tool_ios list-failed "$UNO_ORIGINAL_TEST_RESULTS" "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST"

			export SIMCTL_CHILD_UITEST_RUNTIME_TESTS_FILTER=$(cat "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST" | base64 -b 0)
			export SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE=/tmp/TestResult-rerun-$(date +"%Y%m%d%H%M%S").xml

			xcrun simctl launch "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID"
			RETRY_APP_PID=$(xcrun simctl spawn "$UITEST_IOSDEVICE_ID" launchctl list | grep "$SAMPLESAPP_BUNDLE_ID" | awk '{print $1}')

			RETRY_HB_LOG=$(mktemp /tmp/uno-rt-heartbeat-retry.XXXXXX)
			xcrun simctl spawn "$UITEST_IOSDEVICE_ID" log stream \
				--predicate 'eventMessage CONTAINS "UNO-RT-HEARTBEAT"' \
				>> "$RETRY_HB_LOG" &
			RETRY_STREAM_PID=$!

			ios_wait_for_result "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" \
				"$RETRY_APP_PID" "$((SECONDS + TIMEOUT))" "$RETRY_HB_LOG" "$RETRY_STREAM_PID"

			if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
				run_nunit_tool_ios merge-results "$UNO_ORIGINAL_TEST_RESULTS" "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" "$UNO_ORIGINAL_TEST_RESULTS"
				run_nunit_tool_ios list-failed "$UNO_ORIGINAL_TEST_RESULTS" "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST"
			else
				echo "##[warning]Rerun did not produce a results file; original results kept."
			fi
		fi

	elif [ -n "$IOS_HUNG_TEST_NAME" ]; then
		# Watchdog fired: create a synthetic failure record then rerun the shard excluding the hung test.
		IOS_HUNG_SIMPLE=$(echo "$IOS_HUNG_TEST_NAME" | sed 's/(.*//')
		echo "##[warning]Creating synthetic result for hung test '$IOS_HUNG_SIMPLE' (${IOS_HUNG_HB_AGE}s without heartbeat)."
		run_nunit_tool_ios create-hung-result "$IOS_HUNG_SIMPLE" "$UNO_ORIGINAL_TEST_RESULTS" "$IOS_HUNG_HB_AGE"

		echo "##[warning]Watchdog retry: rerunning shard excluding '$IOS_HUNG_SIMPLE'..."
		export SIMCTL_CHILD_UITEST_RUNTIME_TESTS_FILTER=$(printf '~%s' "$IOS_HUNG_SIMPLE" | base64 -b 0)
		export SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE=/tmp/TestResult-watchdog-retry-$(date +"%Y%m%d%H%M%S").xml

		xcrun simctl launch "$UITEST_IOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID"
		RETRY_APP_PID=$(xcrun simctl spawn "$UITEST_IOSDEVICE_ID" launchctl list | grep "$SAMPLESAPP_BUNDLE_ID" | awk '{print $1}')

		RETRY_HB_LOG=$(mktemp /tmp/uno-rt-heartbeat-retry.XXXXXX)
		xcrun simctl spawn "$UITEST_IOSDEVICE_ID" log stream \
			--predicate 'eventMessage CONTAINS "UNO-RT-HEARTBEAT"' \
			>> "$RETRY_HB_LOG" &
		RETRY_STREAM_PID=$!

		ios_wait_for_result "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" \
			"$RETRY_APP_PID" "$((SECONDS + TIMEOUT))" "$RETRY_HB_LOG" "$RETRY_STREAM_PID"

		if [ -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" ]; then
			run_nunit_tool_ios merge-results "$UNO_ORIGINAL_TEST_RESULTS" "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" "$UNO_ORIGINAL_TEST_RESULTS"
			run_nunit_tool_ios list-failed "$UNO_ORIGINAL_TEST_RESULTS" "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST"
		else
			echo "##[warning]Watchdog retry did not produce a results file; original results kept."
		fi

	else
		echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is not available, the test run has timed out."
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
	## Fail the build when no runtime test results could be read.
	## Use UNO_ORIGINAL_TEST_RESULTS which holds the merged result after a retry,
	## or the first-run copy when no retry was triggered.
	dotnet run fail-empty $UNO_ORIGINAL_TEST_RESULTS

	if [ $? -eq 0 ]; then
		dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_RUNTIMETESTS_FAILED_LIST
	fi
fi

popd
