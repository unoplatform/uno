#!/usr/bin/env bash
set -euo pipefail

# echo commands
set -x

export UITEST_IS_LOCAL=${UITEST_IS_LOCAL=false}
export UNO_UITEST_APP_ID="${UNO_UITEST_APP_ID=uno.platform.samplesapp.skia}"
export UNO_UITEST_ANDROIDAPK_PATH=$BUILD_SOURCESDIRECTORY/build/$SAMPLEAPP_ARTIFACT_NAME/android/$UNO_UITEST_APP_ID-Signed.apk
export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP=automated}
export UNO_ORIGINAL_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-android-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt

export LOGS_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/android-skia-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME
mkdir -p $LOGS_PATH

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -w 0`

	# echo the failed filter list, if not empty
	if [ -n "$UNO_TESTS_FAILED_LIST" ]; then
		echo "Tests to run: $UNO_TESTS_FAILED_LIST"
	fi
else
	# Set to non empty so adb does not complain
	export UITEST_RUNTIME_TESTS_FILTER="fA=="
fi

cd $BUILD_SOURCESDIRECTORY/build

export ANDROID_HOME=$BUILD_SOURCESDIRECTORY/build/android-sdk
export CMDLINETOOLS=commandlinetools-mac-8512546_latest.zip # works on Linux instances as well, somehow

if [[ ! -d $ANDROID_HOME ]];
then
	mkdir -p $ANDROID_HOME
	wget https://dl.google.com/android/repository/$CMDLINETOOLS
	unzip $CMDLINETOOLS -d $ANDROID_HOME/cmdline-tools
	rm $CMDLINETOOLS
	mv $ANDROID_HOME/cmdline-tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest
fi

AVD_NAME=xamarin_android_emulator
AVD_CONFIG_FILE=~/.android/avd/$AVD_NAME.avd/config.ini

if [[ ! -f $AVD_CONFIG_FILE ]];
then
	# Create a variable that sets x86_64 or aarch64 for the emulator based on the current architecture.
	# Used for local tested.
	EMU_ARCH=x86_64
	if [[ $(uname -m) == "arm64" ]]; then
		EMU_ARCH=arm64-v8a
	fi

	# Install AVD files
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'tools'| tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'platform-tools'  | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'build-tools;35.0.0' | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'platforms;android-28' | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'extras;android;m2repository' | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install "system-images;android-28;google_apis_playstore;$EMU_ARCH" | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis_playstore;$EMU_ARCH" | tr '\r' '\n' | uniq

	# Create emulator
	echo "no" | $ANDROID_HOME/cmdline-tools/latest/bin/avdmanager create avd -n "$AVD_NAME" --abi $EMU_ARCH -k "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis_playstore;$EMU_ARCH" --sdcard 128M --force

	# Bump the heap size as the tests are stressing the application
	echo "vm.heapSize=256M" >> $AVD_CONFIG_FILE

	# Force the orentation to landscape as most tests expect it to be this way
	echo "hw.initialOrientation=landscape" >> $AVD_CONFIG_FILE

	echo $ANDROID_HOME/emulator/emulator -list-avds

	echo "Checking for hardware acceleration"
	$ANDROID_HOME/emulator/emulator -accel-check

	echo "Starting emulator"

	# kickstart ADB
	$ANDROID_HOME/platform-tools/adb devices

	# Show the emulator when running locally
	if [ "$UITEST_IS_LOCAL" == "true" ];
	then
		export EMU_NO_WINDOW=""
	else
		export EMU_NO_WINDOW="-no-window"
	fi

	# Start emulator in background
	nohup $ANDROID_HOME/emulator/emulator \
		-avd "$AVD_NAME" \
		-skin 1280x800 \
		-memory 4096 \
		$EMU_NO_WINDOW \
		-gpu swiftshader_indirect \
		-no-snapshot \
		-noaudio \
		-no-boot-anim \
		-prop ro.debuggable=1 \
		> $LOGS_PATH/android-emulator-log-$UNO_UITEST_BUCKET_ID-$UITEST_TEST_MODE_NAME.txt 2>&1 &

	# Wait for the emulator to finish booting
	source $BUILD_SOURCESDIRECTORY/build/test-scripts/android-uitest-wait-systemui.sh 500

else
	# Restart the emulator to avoid running first-time tasks
	$ANDROID_HOME/platform-tools/adb reboot

	# Wait for the emulator to finish booting
	source $BUILD_SOURCESDIRECTORY/build/test-scripts/android-uitest-wait-systemui.sh 500
fi

# list active devices
$ANDROID_HOME/platform-tools/adb devices

echo "Emulator started"

# Install the app
$ANDROID_HOME/platform-tools/adb install $UNO_UITEST_ANDROIDAPK_PATH

NUNIT_TOOL_DIR="$BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"
run_nunit_tool() { (cd "$NUNIT_TOOL_DIR" && dotnet run "$@"); }

# Maximum failures to trigger an in-job retry; overridable via env var.
FLAKE_RETRY_MAX_FAILURES=${FLAKE_RETRY_MAX_FAILURES:-20}

# Maximum seconds between heartbeat emissions before the watchdog kills the app.
HEARTBEAT_TIMEOUT_S=${HEARTBEAT_TIMEOUT_S:-300}

# Set by run_android_tests when the heartbeat watchdog fires.
HUNG_TEST_NAME=""
HUNG_HB_AGE=0

UITEST_RUNTIME_AUTOSTART_RESULT_DEVICE_BASE_PATH="/storage/emulated/0/Android/data/$UNO_UITEST_APP_ID/files"

# Starts the app and waits for the result file to appear on device.
# Arguments:
#   $1  filter (base64-encoded test name list)
#   $2  local output path for the XML result file
run_android_tests() {
	local filter="$1"
	local local_result_path="$2"
	local result_filename
	result_filename="TestResult-$(date +"%Y%m%d%H%M%S").xml"
	local device_result_path="$UITEST_RUNTIME_AUTOSTART_RESULT_DEVICE_BASE_PATH/$result_filename"

	# grant the storage permission to the app to write the test results and read the environment file
	$ANDROID_HOME/platform-tools/adb shell pm grant $UNO_UITEST_APP_ID android.permission.WRITE_EXTERNAL_STORAGE
	$ANDROID_HOME/platform-tools/adb shell pm grant $UNO_UITEST_APP_ID android.permission.READ_EXTERNAL_STORAGE

	# Clear logcat so heartbeat line counting starts fresh for this run.
	$ANDROID_HOME/platform-tools/adb logcat -c

	# start the android app using environment variables using adb
	$ANDROID_HOME/platform-tools/adb shell am start \
	  -n $UNO_UITEST_APP_ID/crc6448f3b0362cbf4bc9.MainActivity \
	  -e UITEST_RUNTIME_TEST_GROUP "$UITEST_RUNTIME_TEST_GROUP" \
	  -e UITEST_RUNTIME_TEST_GROUP_COUNT "$UITEST_RUNTIME_TEST_GROUP_COUNT" \
	  -e UITEST_RUNTIME_AUTOSTART_RESULT_FILE "$device_result_path" \
	  -e UITEST_RUNTIME_TESTS_FILTER "$filter" \

	# Set the timeout in seconds
	local UITEST_TEST_TIMEOUT_AS_MINUTES=${UITEST_TEST_TIMEOUT:0:${#UITEST_TEST_TIMEOUT}-1}
	local TIMEOUT=$(($UITEST_TEST_TIMEOUT_AS_MINUTES * 60))
	local END_TIME=$((SECONDS+TIMEOUT))

	echo "Waiting for $device_result_path to be available..."

	local PREV_HB_COUNT=0
	local LAST_HB_S=$SECONDS

	while [[ ! $($ANDROID_HOME/platform-tools/adb shell test -e "$device_result_path" > /dev/null) && $SECONDS -lt $END_TIME ]]; do
	    sleep 15

		## Dump the emulator's system log
		$ANDROID_HOME/platform-tools/adb shell logcat -d > $LOGS_PATH/android-device-log-$UNO_UITEST_BUCKET_ID-$UITEST_RUNTIME_TEST_GROUP-$UITEST_TEST_MODE_NAME.interim.txt

		# Heartbeat watchdog: count heartbeat lines in logcat; if count stalls, the test may be hung.
		local HB_COUNT
		HB_COUNT=$($ANDROID_HOME/platform-tools/adb shell logcat -d | grep -c 'UNO-RT-HEARTBEAT' || echo 0)
		if [ "$HB_COUNT" -gt "$PREV_HB_COUNT" ]; then
			PREV_HB_COUNT=$HB_COUNT
			LAST_HB_S=$SECONDS
		fi
		local HB_AGE=$(( SECONDS - LAST_HB_S ))
		if [ "$PREV_HB_COUNT" -gt 0 ] && [ $HB_AGE -gt $HEARTBEAT_TIMEOUT_S ]; then
			echo "##[error]No heartbeat for ${HB_AGE}s — test appears hung."
			# Print the last 3 heartbeats for context
			$ANDROID_HOME/platform-tools/adb shell logcat -d | grep 'UNO-RT-HEARTBEAT' | tail -3

			# Extract the name of the hung test from the last STARTING heartbeat without a matching COMPLETED
			local last_starting
			last_starting=$($ANDROID_HOME/platform-tools/adb shell logcat -d \
				| grep 'UNO-RT-HEARTBEAT' | grep 'STARTING' | tail -1 \
				| sed 's/.*STARTING //')
			if [ -n "$last_starting" ]; then
				HUNG_TEST_NAME="$last_starting"
				HUNG_HB_AGE=$HB_AGE
				echo "##[error]Hung test identified: $HUNG_TEST_NAME (no heartbeat for ${HB_AGE}s)"
			fi

			$ANDROID_HOME/platform-tools/adb shell am force-stop "$UNO_UITEST_APP_ID" || true
			break
		fi

	    # exit loop early if the app is not running anymore
	    if ! $ANDROID_HOME/platform-tools/adb shell ps | grep "$UNO_UITEST_APP_ID" > /dev/null; then
	        echo "The app is not running anymore"
	        break
	    fi
	done

	$ANDROID_HOME/platform-tools/adb pull $device_result_path "$local_result_path" || echo "ERROR: could not adb pull $device_result_path"

	## Dump the emulator's system log
	$ANDROID_HOME/platform-tools/adb shell logcat -d > $LOGS_PATH/android-device-log-$UNO_UITEST_BUCKET_ID-$UITEST_RUNTIME_TEST_GROUP-$UITEST_TEST_MODE_NAME.txt
}

# --- First run ---

UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH="$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml"
run_android_tests "$UITEST_RUNTIME_TESTS_FILTER" "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH"

# create $BUILD_SOURCESDIRECTORY/build/uitests-failure-results before exiting, so that `PublishBuildArtifacts@1` doesn't error out just because the tests crashed.
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

# When the watchdog fires the result file is never written; synthesise a failure entry
# so the rest of the pipeline has something to work with.
if [ ! -f "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH" ] && [ -n "$HUNG_TEST_NAME" ]; then
	HUNG_SIMPLE=$(echo "$HUNG_TEST_NAME" | sed 's/(.*//')
	echo "##[warning]Creating synthetic result for hung test '$HUNG_SIMPLE' (${HUNG_HB_AGE}s without heartbeat)."
	run_nunit_tool create-hung-result "$HUNG_SIMPLE" "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH" "$HUNG_HB_AGE"
fi

if [ ! -f "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH" ]; then
	echo "ERROR: The test results file $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH does not exist (did nunit crash ?)"
	exit 1
fi

## Fail the build when no test results could be read
run_nunit_tool fail-empty $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH

FAILED_COUNT=$(run_nunit_tool count-failed $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH | tail -1)
run_nunit_tool list-failed $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH $UNO_TESTS_FAILED_LIST

RERUN_RESULT_PATH="$BUILD_SOURCESDIRECTORY/build/TestResult-rerun.xml"

if [ -n "$HUNG_TEST_NAME" ]; then
	# Watchdog fired: rerun the full shard but skip the hung test so we get results for
	# all the tests that never ran.  The hung test remains marked Failed in the merged output.
	HUNG_SIMPLE=$(echo "$HUNG_TEST_NAME" | sed 's/(.*//')
	echo "##[warning]Watchdog retry: rerunning shard excluding '$HUNG_SIMPLE'..."
	RERUN_FILTER=$(printf '~%s' "$HUNG_SIMPLE" | base64 -w 0)

	run_android_tests "$RERUN_FILTER" "$RERUN_RESULT_PATH"

	if [ -f "$RERUN_RESULT_PATH" ]; then
		run_nunit_tool merge-results "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH" "$RERUN_RESULT_PATH" "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH"
		run_nunit_tool list-failed "$UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH" "$UNO_TESTS_FAILED_LIST"
	else
		echo "##[warning]Watchdog retry did not produce a results file; original results kept."
	fi

# In-job retry: if a small number of tests failed, rerun only those to catch flakes
elif [ "$FAILED_COUNT" -gt 0 ] && [ "$FAILED_COUNT" -le "$FLAKE_RETRY_MAX_FAILURES" ]; then
	echo "##[warning]$FAILED_COUNT test(s) failed — retrying in-job to filter flakes..."

	RERUN_FILTER=$(cat $UNO_TESTS_FAILED_LIST | base64 -w 0)

	run_android_tests "$RERUN_FILTER" "$RERUN_RESULT_PATH"

	if [ -f "$RERUN_RESULT_PATH" ]; then
		run_nunit_tool merge-results $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH $RERUN_RESULT_PATH $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH
		run_nunit_tool list-failed $UITEST_RUNTIME_AUTOSTART_RESULT_LOCAL_PATH $UNO_TESTS_FAILED_LIST
	else
		echo "##[warning]Rerun did not produce a results file; original results kept."
	fi
fi
