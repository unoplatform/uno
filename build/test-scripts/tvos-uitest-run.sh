#!/bin/bash
# ===========================================================================
# tvos-uitest-run.sh — CI (Azure DevOps) entry point that runs the SamplesApp
# runtime tests against a tvOS Simulator on a macOS hosted agent.
#
# Unlike ios-uitest-run.sh, this script only ever runs the RuntimeTests group.
# That mode never drives the UI: the app is launched with the runtime-test
# harness variables set, writes its NUnit results to a file, and exits. So the
# whole Xamarin.UITest / NUnit console path is unnecessary here, and so is idb
# (Meta's iOS Development Bridge) — `xcrun simctl install` is enough, and idb
# has no tvOS support to fall back on anyway.
#
# What it does:
#   1. Resolves a tvOS simulator, creating one if the image ships none.
#   2. Boots it, and installs the pre-built SamplesApp bundle.
#   3. Launches the app with the SIMCTL_CHILD_* harness variables, then waits
#      for the results file to appear (the simulator shares the host's /tmp).
#   4. Transforms the results and maintains the failed-tests re-run list.
# ===========================================================================
set -euo pipefail
IFS=$'\n\t'

# The pipeline has a re-run step that is only reachable when the harness died before any test
# started — a boot or install failure is worth one more attempt, unlike a genuine test failure,
# which would not fit the job budget a second time. This sentinel marks that case.
UNO_TVOS_TESTS_STARTED=false

report_harness_crash() {
	local status=$?

	if [ "$status" -ne 0 ] && [ "$UNO_TVOS_TESTS_STARTED" != "true" ]; then
		echo "##vso[task.setvariable variable=UNO_TVOS_HARNESS_CRASHED]true"
	fi
}
trap report_harness_crash EXIT

# Defaults so a local run (or a future caller that only cares about a single group)
# does not trip `set -u` on the harness variables the pipeline normally supplies.
UITEST_RUNTIME_TEST_GROUP="${UITEST_RUNTIME_TEST_GROUP:=0}"
UITEST_RUNTIME_TEST_GROUP_COUNT="${UITEST_RUNTIME_TEST_GROUP_COUNT:=1}"
UITEST_TEST_TIMEOUT="${UITEST_TEST_TIMEOUT:=90m}"

export SCREENSHOTS_FOLDERNAME=tvos

if [ -n "${UITEST_VARIANT-}" ]; then
	export SCREENSHOTS_FOLDERNAME="$SCREENSHOTS_FOLDERNAME-$UITEST_VARIANT"
fi

export LOG_FILEPATH=$BUILD_SOURCESDIRECTORY/tvos-ui-tests-logs/$SCREENSHOTS_FOLDERNAME/_logs
export LOG_PREFIX=`date +"%Y%m%d%H%M%S"`

# Create the log directory early so that the artifacts publish task works properly
mkdir -p $LOG_FILEPATH

export UNO_UITEST_PLATFORM=tvOS
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME

export UNO_ORIGINAL_TEST_RESULTS_DIRECTORY=$BUILD_SOURCESDIRECTORY/build
export UNO_ORIGINAL_TEST_RESULTS=$UNO_ORIGINAL_TEST_RESULTS_DIRECTORY/TestResult-original.xml
export UNO_TESTS_RUNTIMETESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-runtime-tests-tvos-$SCREENSHOTS_FOLDERNAME-${UITEST_RUNTIME_TEST_GROUP}.txt

## Create the failed-tests directory up front, next to the log directory above: an abort
## between here and the transform tool (a failed install, a sick simulator during teardown)
## otherwise leaves `PublishBuildArtifacts@1` retrying a missing PathtoPublish.
mkdir -p $(dirname ${UNO_TESTS_RUNTIMETESTS_FAILED_LIST})
mkdir -p $UNO_UITEST_SCREENSHOT_PATH

_TFM="${TFM:=net10.0-tvos}"
export UnoTargetFrameworkOverride="$_TFM"

UITEST_IGNORE_RERUN_FILE="${UITEST_IGNORE_RERUN_FILE:=false}"

echo "Current system date"
date

##
## Resolve the simulator. The iOS suite pins a runtime/device pair, but there is no tvOS runtime
## guaranteed to be present across the hosted images, so take whichever one Xcode brought and
## create a device for it when the image ships the runtime but no Apple TV device.
##
UNO_UITEST_SIMULATOR_NAME="${UNO_UITEST_SIMULATOR_NAME:=Apple TV}"

find_tvos_device() {
	xcrun simctl list devices --json | jq -r --arg name "$UNO_UITEST_SIMULATOR_NAME" '
		.devices
		| to_entries
		| map(select(.key | test("SimRuntime\\.tvOS")))
		| map(.value[] | select(.isAvailable == true))
		| (map(select(.name == $name)) + .)
		| .[0].udid // empty'
}

# The runtime may still be finishing its install, so give it a few tries before creating a device.
UITEST_TVOSDEVICE_ID=""
for attempt in 1 2 3 4 5 6; do
	UITEST_TVOSDEVICE_ID=$(find_tvos_device)

	if [ -n "$UITEST_TVOSDEVICE_ID" ]; then
		break
	fi

	echo "Waiting for a tvOS simulator to be available (attempt $attempt)"
	sleep 5
done

if [ -z "$UITEST_TVOSDEVICE_ID" ]; then
	echo "No tvOS simulator device found, creating one."

	TVOS_RUNTIME_ID=$(xcrun simctl list runtimes --json | jq -r '
		.runtimes
		| map(select(.isAvailable == true and (.identifier | test("SimRuntime\\.tvOS"))))
		| .[-1].identifier // empty')
	TVOS_DEVICETYPE_ID=$(xcrun simctl list devicetypes --json | jq -r '
		.devicetypes
		| map(select(.identifier | test("SimDeviceType\\.Apple-TV")))
		| .[-1].identifier // empty')

	if [ -z "$TVOS_RUNTIME_ID" ] || [ -z "$TVOS_DEVICETYPE_ID" ]; then
		echo "##vso[task.logissue type=error]UNOBLD008: No tvOS simulator runtime or device type is available on this agent."
		xcrun simctl list runtimes || true
		xcrun simctl list devicetypes || true
		exit 1
	fi

	echo "Creating '$UNO_UITEST_SIMULATOR_NAME' ($TVOS_DEVICETYPE_ID / $TVOS_RUNTIME_ID)"
	UITEST_TVOSDEVICE_ID=$(xcrun simctl create "$UNO_UITEST_SIMULATOR_NAME" "$TVOS_DEVICETYPE_ID" "$TVOS_RUNTIME_ID")
fi

export UITEST_TVOSDEVICE_ID

export DEVICELIST_FILEPATH=$LOG_FILEPATH/DeviceList-$LOG_PREFIX.json
echo "Listing tvOS simulators to $DEVICELIST_FILEPATH"
xcrun simctl list devices --json > $DEVICELIST_FILEPATH

echo "Starting simulator: [$UITEST_TVOSDEVICE_ID] ($UNO_UITEST_SIMULATOR_NAME)"
xcrun simctl boot "$UITEST_TVOSDEVICE_ID" || true

# `xcrun simctl bootstatus -b` blocks until the device reports a finished boot, but it has no
# timeout of its own and macOS ships no timeout(1). Run it under a watchdog, so a simulator that
# never finishes booting is reported here instead of as an opaque failure further down.
wait_for_boot() {
	local udid="$1"
	local limit="$2"
	local status=0

	xcrun simctl bootstatus "$udid" -b &
	local boot_pid=$!

	( sleep "$limit"; kill -TERM "$boot_pid" 2>/dev/null ) &
	local watchdog_pid=$!

	wait "$boot_pid" || status=$?

	kill -TERM "$watchdog_pid" 2>/dev/null || true
	wait "$watchdog_pid" 2>/dev/null || true

	return $status
}

echo "Waiting for the simulator to finish booting (started $(date))"
if ! wait_for_boot "$UITEST_TVOSDEVICE_ID" 180; then
	echo "##vso[task.logissue type=warning]UNOBLD006: The simulator did not report a completed boot within 180s. Continuing anyway; the app install below will surface a hard failure if it is genuinely unusable."
fi
echo "Simulator boot wait finished ($(date))"

# Imported app bundle from artifacts is not executable
sudo chmod -R +x $UNO_UITEST_TVOSBUNDLE_PATH

echo "Installing app on simulator: $UITEST_TVOSDEVICE_ID"
xcrun simctl install "$UITEST_TVOSDEVICE_ID" "$UNO_UITEST_TVOSBUNDLE_PATH"

## Pre-build the transform tool to get early warnings
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
dotnet build
popd

cd $BUILD_SOURCESDIRECTORY/build

export SIMCTL_CHILD_UITEST_RUNTIME_TEST_GROUP=$UITEST_RUNTIME_TEST_GROUP
export SIMCTL_CHILD_UITEST_RUNTIME_TEST_GROUP_COUNT=$UITEST_RUNTIME_TEST_GROUP_COUNT
export SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE=/tmp/TestResult-`date +"%Y%m%d%H%M%S"`.xml

if [ -f "$UNO_TESTS_RUNTIMETESTS_FAILED_LIST" ] && [ "$UITEST_IGNORE_RERUN_FILE" != "true" ]; then

	# if it only contains `invalid-test-for-retry`, exit the script
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

echo "Starting runtime tests group ${UITEST_RUNTIME_TEST_GROUP} of ${UITEST_RUNTIME_TEST_GROUP_COUNT}"

UNO_TVOS_TESTS_STARTED=true
xcrun simctl launch "$UITEST_TVOSDEVICE_ID" "$SAMPLESAPP_BUNDLE_ID"

# get the process id for the app
export APP_PID=`xcrun simctl spawn "$UITEST_TVOSDEVICE_ID" launchctl list | grep "$SAMPLESAPP_BUNDLE_ID" | awk '{print $1}'`
echo "App PID: $APP_PID"

# Set the timeout in seconds
UITEST_TEST_TIMEOUT_AS_MINUTES=${UITEST_TEST_TIMEOUT:0:${#UITEST_TEST_TIMEOUT}-1}
TIMEOUT=$(($UITEST_TEST_TIMEOUT_AS_MINUTES * 60))
INTERVAL=15
END_TIME=$((SECONDS+TIMEOUT))

echo "Waiting for $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE to be available..."

# Loop until the file exists or the timeout is reached
while [[ ! -f "$SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE" && $SECONDS -lt $END_TIME ]]; do
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
else
	echo "The file $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE is not available, the test run has timed out."
fi

# export the simulator logs
export TMP_LOG_FILEPATH=/tmp/DeviceLog-$LOG_PREFIX.logarchive
export LOG_FILE_DIRECTORY=$LOG_FILEPATH/RuntimeTests-${UITEST_RUNTIME_TEST_GROUP}-`date +"%Y%m%d%H%M%S"`
export LOG_FILEPATH_FULL=$LOG_FILE_DIRECTORY/DeviceLog-`date +"%Y%m%d%H%M%S"`.txt

mkdir -p $LOG_FILE_DIRECTORY

cp -fv "$UNO_ORIGINAL_TEST_RESULTS" $LOG_FILEPATH/Test-Results-$LOG_PREFIX.xml || true

# Teardown is best-effort: under `set -e` a sick simulator failing any of these aborts the
# script before the results are transformed and published, turning a diagnosable test failure
# into a bare non-zero exit.
## Take a screenshot
xcrun simctl io "$UITEST_TVOSDEVICE_ID" screenshot $LOG_FILEPATH/capture-$LOG_PREFIX.png || true

## Capture the device logs
xcrun simctl spawn "$UITEST_TVOSDEVICE_ID" log collect --output $TMP_LOG_FILEPATH || true

## Shutting down simulator to reclaim memory
echo "Shutting down simulator"
xcrun simctl shutdown "$UITEST_TVOSDEVICE_ID" || true

echo "Dumping device logs to $LOG_FILEPATH_FULL"
log show --style syslog $TMP_LOG_FILEPATH > $LOG_FILEPATH_FULL || true

echo "Searching for failures in device logs"
if [ ! -s "$LOG_FILEPATH_FULL" ]; then
	echo "Device log is empty or missing; skipping the log scans"
	: > "$LOG_FILEPATH_FULL"
fi
if grep -Eq "mini-generic-sharing.c:[0-9]+, condition .oti. not met" $LOG_FILEPATH_FULL
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
	echo "##vso[task.logissue type=error]UNOBLD003: ERROR: The test results file $UNO_ORIGINAL_TEST_RESULTS does not exist (did the app crash ?)"
fi

echo "Copying crash reports"
cp -R ~/Library/Logs/DiagnosticReports/* $LOG_FILE_DIRECTORY || true

pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool

echo "Running NUnitTransformTool"

## Fail the build when no runtime test results could be read
dotnet run fail-empty $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE $UNO_TESTS_RUNTIMETESTS_FAILED_LIST
fi

popd
