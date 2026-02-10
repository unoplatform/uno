#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'

# echo commands
set -x

export BUILDCONFIGURATION=Release

if [ "$UITEST_TEST_MODE_NAME" == 'Snapshots' ];
then
	export TEST_FILTERS="FullyQualifiedName ~ SamplesApp.UITests.Snap"

	export SCREENSHOTS_FOLDERNAME=android-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME-Snap

elif [ "$UITEST_TEST_MODE_NAME" == 'Automated' ];
then
	export TEST_FILTERS="\
		Namespace !~ SamplesApp.UITests.Snap\
		& FullyQualifiedName !~ SamplesApp.UITests.Runtime.BenchmarkDotNetTests\
		& FullyQualifiedName !~ SamplesApp.UITests.Runtime.RuntimeTests\
		& Category~testBucket:$UNO_UITEST_BUCKET_ID\
	";
	export SCREENSHOTS_FOLDERNAME=android-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME

elif [ "$UITEST_TEST_MODE_NAME" == 'RuntimeTests' ];
then
	export TEST_FILTERS="FullyQualifiedName ~ SamplesApp.UITests.Runtime.RuntimeTests"

	export SCREENSHOTS_FOLDERNAME=android-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME
fi

export UITEST_IS_LOCAL=${UITEST_IS_LOCAL=false}
export UNO_ANDROID_BUILD_TOOLS_VERSION="35.0.1"
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME
export UNO_UITEST_APP_ID="uno.platform.unosampleapp"
export UNO_UITEST_PLATFORM=Android
export UNO_UITEST_ANDROIDAPK_PATH=$BUILD_SOURCESDIRECTORY/build/$SAMPLEAPP_ARTIFACT_NAME/android/uno.platform.unosampleapp-Signed.apk
export IsUiAutomationMappingEnabled=true
export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP=automated}
export UNO_TESTS_LOCAL_TESTS_FILE=$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests
export UNO_ORIGINAL_TEST_RESULTS_DIRECTORY=$BUILD_SOURCESDIRECTORY/build
export UNO_ORIGINAL_TEST_RESULTS=$UNO_ORIGINAL_TEST_RESULTS_DIRECTORY/TestResult-original.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-android-$ANDROID_SIMULATOR_APILEVEL-$SCREENSHOTS_FOLDERNAME-$UNO_UITEST_BUCKET_ID-$UITEST_RUNTIME_TEST_GROUP-$TARGETPLATFORM_NAME.txt
export UNO_TESTS_RESPONSE_FILE=$BUILD_SOURCESDIRECTORY/build/nunit.response
export UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH=$BUILD_SOURCESDIRECTORY/build/RuntimeTestResults-android-automated-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME.xml

mkdir -p $UNO_UITEST_SCREENSHOT_PATH
# Always create the failed-results folder so artifact publishing does not fail on success.
mkdir -p $BUILD_SOURCESDIRECTORY/build/uitests-failure-results

if [ -f "$UNO_TESTS_FAILED_LIST" ] && [ `cat "$UNO_TESTS_FAILED_LIST"` = "invalid-test-for-retry" ];
then
	# The test results file only contains the re-run marker and no
	# other test to rerun. We can skip this run.
	echo "The file $UNO_TESTS_FAILED_LIST does not contain tests to re-run, skipping."
	exit 0
fi

cd $BUILD_SOURCESDIRECTORY/build

# This block allows to override the Android SDK
# disabled until hosted agents move to macOS 11
#
export ANDROID_HOME=$BUILD_SOURCESDIRECTORY/build/android-sdk
export ANDROID_SDK_ROOT=$BUILD_SOURCESDIRECTORY/build/android-sdk
export CMDLINETOOLS=commandlinetools-linux-8512546_latest.zip
export ANDROID_AVD_HOME="${ANDROID_AVD_HOME:=$HOME/.android/avd}"

if [[ ! -d $ANDROID_HOME ]];
then
	mkdir -p $ANDROID_HOME
	wget https://dl.google.com/android/repository/$CMDLINETOOLS
	unzip $CMDLINETOOLS -d $ANDROID_HOME/cmdline-tools
	rm $CMDLINETOOLS
	mv $ANDROID_SDK_ROOT/cmdline-tools/cmdline-tools $ANDROID_SDK_ROOT/cmdline-tools/latest
fi

# uncomment the following lines to override the installed Xamarin.Android SDK
# wget -nv https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android-d16-2/49/Azure/processDownloadRequest/xamarin-android/xamarin-android/bin/BuildRelease/Xamarin.Android.Sdk-OSS-9.4.0.59_d16-2_6d9b105.pkg
# sudo installer -verbose -pkg Xamarin.Android.Sdk-OSS-9.4.0.59_d16-2_6d9b105.pkg -target /

AVD_NAME=xamarin_android_emulator
AVD_CONFIG_FILE="$ANDROID_AVD_HOME/$AVD_NAME.avd/config.ini"

is_emulator_booted() {
	if $ANDROID_HOME/platform-tools/adb devices | grep -q "emulator"; then
		local boot_completed
		boot_completed=$($ANDROID_HOME/platform-tools/adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r' || true)
		[ "$boot_completed" = "1" ]
	else
		return 1
	fi
}

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
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install "build-tools;$UNO_ANDROID_BUILD_TOOLS_VERSION" | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'platforms;android-28' | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'extras;android;m2repository' | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install "system-images;android-28;google_apis_playstore;$EMU_ARCH" | tr '\r' '\n' | uniq
	echo "y" | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis_playstore;$EMU_ARCH" | tr '\r' '\n' | uniq

	if [[ -f $ANDROID_HOME/platform-tools/platform-tools/adb ]]
	then
		# It appears that the platform-tools 29.0.6 are extracting into an incorrect path
		mv $ANDROID_HOME/platform-tools/platform-tools/* $ANDROID_HOME/platform-tools
	fi

	# Create emulator
	echo "no" | $ANDROID_HOME/cmdline-tools/latest/bin/avdmanager create avd -n "$AVD_NAME" --abi $EMU_ARCH -k "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis_playstore;$EMU_ARCH" --sdcard 128M --force

	# based on https://docs.microsoft.com/en-us/azure/devops/pipelines/agents/hosted?view=azure-devops&tabs=yaml#hardware
	# >> Agents that run macOS images are provisioned on Mac pros with a 3 core CPU, 14 GB of RAM, and 14 GB of SSD disk space.
	echo "hw.cpu.ncore=3" >> $AVD_CONFIG_FILE

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
		> $BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME/android-emulator-log-$UNO_UITEST_BUCKET_ID-$UITEST_TEST_MODE_NAME.txt 2>&1 &

	# Wait for the emulator to finish booting
	source $BUILD_SOURCESDIRECTORY/build/test-scripts/android-uitest-wait-systemui.sh 500

else
	if is_emulator_booted; then
		# Reuse the already-booted emulator during reruns to save startup time.
		echo "Emulator already booted; skipping reboot."
	else
		# Restart the emulator to avoid running first-time tasks
		$ANDROID_HOME/platform-tools/adb reboot

		# Wait for the emulator to finish booting
		source $BUILD_SOURCESDIRECTORY/build/test-scripts/android-uitest-wait-systemui.sh 500
	fi
fi

# list active devices
$ANDROID_HOME/platform-tools/adb devices

# Workaround for https://github.com/microsoft/appcenter/issues/1451
$ANDROID_HOME/platform-tools/adb shell settings put global hidden_api_policy 1

echo "Emulator started"

cp $UNO_UITEST_ANDROIDAPK_PATH $BUILD_ARTIFACTSTAGINGDIRECTORY

cd $BUILD_SOURCESDIRECTORY/build

# Move to the screenshot directory so that the output path is the proper one, as
# required by Xamarin.UITest
cd $UNO_UITEST_SCREENSHOT_PATH

if [ "$UITEST_TEST_MODE_NAME" == 'RuntimeTests' ];
then
	UITEST_RUNTIME_AUTOSTART_RESULT_FILE="/sdcard/TestResult-`date +"%Y%m%d%H%M%S"`.xml"
	UITEST_RUNTIME_CURRENT_TEST_FILE="/sdcard/RuntimeCurrentTest-$UITEST_RUNTIME_TEST_GROUP.txt"

	# Install the app
	$ANDROID_HOME/platform-tools/adb install $UNO_UITEST_ANDROIDAPK_PATH

	# Create the environment file for the app to read
	echo "UITEST_RUNTIME_TEST_GROUP=$UITEST_RUNTIME_TEST_GROUP" > samplesapp-environment.txt
	echo "UITEST_RUNTIME_TEST_GROUP_COUNT=$UITEST_RUNTIME_TEST_GROUP_COUNT" >> samplesapp-environment.txt
		echo "UITEST_RUNTIME_AUTOSTART_RESULT_FILE=$UITEST_RUNTIME_AUTOSTART_RESULT_FILE" >> samplesapp-environment.txt
		# CI uses this file to report the last started test when a shard hangs.
		echo "UITEST_RUNTIME_CURRENT_TEST_FILE=$UITEST_RUNTIME_CURRENT_TEST_FILE" >> samplesapp-environment.txt

	if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
		export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -w 0`

		echo "UITEST_RUNTIME_TESTS_FILTER=$UITEST_RUNTIME_TESTS_FILTER" >> samplesapp-environment.txt

		# echo the failed filter list, if not empty
		if [ -n "$UNO_TESTS_FAILED_LIST" ]; then
			echo "Tests to run: $UNO_TESTS_FAILED_LIST"
		fi
	fi

	# Push the environment file to the device
	$ANDROID_HOME/platform-tools/adb push samplesapp-environment.txt /sdcard/samplesapp-environment.txt

	# grant the storage permission to the app to write the test results and read the environment file
	$ANDROID_HOME/platform-tools/adb shell pm grant $UNO_UITEST_APP_ID android.permission.WRITE_EXTERNAL_STORAGE
	$ANDROID_HOME/platform-tools/adb shell pm grant $UNO_UITEST_APP_ID android.permission.READ_EXTERNAL_STORAGE

	# start the android app using environment variables using adb
	$ANDROID_HOME/platform-tools/adb shell monkey -p $UNO_UITEST_APP_ID -c android.intent.category.LAUNCHER 1

	# Set the timeout in seconds 
	UITEST_TEST_TIMEOUT_AS_MINUTES=${UITEST_TEST_TIMEOUT:0:${#UITEST_TEST_TIMEOUT}-1}
	TIMEOUT=$(($UITEST_TEST_TIMEOUT_AS_MINUTES * 60))
	INTERVAL=15
	END_TIME=$((SECONDS+TIMEOUT))

	echo "Waiting for $UITEST_RUNTIME_AUTOSTART_RESULT_FILE to be available..."

	while [[ ! $($ANDROID_HOME/platform-tools/adb shell test -e "$UITEST_RUNTIME_AUTOSTART_RESULT_FILE" > /dev/null) && $SECONDS -lt $END_TIME ]]; do
		# echo "Waiting $INTERVAL seconds for test results to be written to $UITEST_RUNTIME_AUTOSTART_RESULT_FILE";
		sleep $INTERVAL

		# exit loop if the APP_PID is not running anymore
		if ! $ANDROID_HOME/platform-tools/adb shell ps | grep "$UNO_UITEST_APP_ID" > /dev/null; then
			echo "The app is not running anymore"
			break
		fi
	done

	if ! $ANDROID_HOME/platform-tools/adb shell test -e "$UITEST_RUNTIME_AUTOSTART_RESULT_FILE" > /dev/null; then
		if [ $SECONDS -ge $END_TIME ]; then
			# Capture a timeout marker so CI artifacts explain why the run ended early.
			echo "Runtime tests timed out after ${TIMEOUT}s (group=$UITEST_RUNTIME_TEST_GROUP)." > $BUILD_SOURCESDIRECTORY/build/uitests-failure-results/runtime-tests-timeout-android-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME-$UITEST_RUNTIME_TEST_GROUP.txt
		else
			echo "Runtime tests ended without results (app exited early)." > $BUILD_SOURCESDIRECTORY/build/uitests-failure-results/runtime-tests-no-results-android-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME-$UITEST_RUNTIME_TEST_GROUP.txt
		fi
	fi

	$ANDROID_HOME/platform-tools/adb pull $UITEST_RUNTIME_AUTOSTART_RESULT_FILE $UNO_ORIGINAL_TEST_RESULTS || true
	
	RUNTIME_CURRENT_TEST_LOCAL="$BUILD_SOURCESDIRECTORY/build/runtime-current-test-android-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME-$UITEST_RUNTIME_TEST_GROUP.txt"
	$ANDROID_HOME/platform-tools/adb pull $UITEST_RUNTIME_CURRENT_TEST_FILE $RUNTIME_CURRENT_TEST_LOCAL || true
	if [ -f "$RUNTIME_CURRENT_TEST_LOCAL" ]; then
		echo "Last runtime test heartbeat: $(cat "$RUNTIME_CURRENT_TEST_LOCAL")"
	else
		echo "No runtime test heartbeat file found."
	fi

else

	if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
		UNO_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST`
	else
		UNO_TESTS_FILTER=$TEST_FILTERS
	fi

	echo "Test Parameters:"
	echo "  Timeout=$UITEST_TEST_TIMEOUT"
	echo "  Test filters: $UNO_TESTS_FILTER"

	cd $UNO_TESTS_LOCAL_TESTS_FILE

	# Response file for testing to avoid the command line length limitation
	# new parameters must include the ":" to separate parameter options
	# the response file contains only the filters, in order to get proper stderr
	echo "--filter:\"$UNO_TESTS_FILTER\"" > tests.rsp

	# Workaround for https://github.com/dotnet/maui/issues/31072
	# Create a fake file that Xamarin.UITest will recognize and keep going
	touch assemblies.blob
	zip -u "$UNO_UITEST_ANDROIDAPK_PATH" assemblies.blob

	## Run NUnit tests
	dotnet run -c Release -bl:$UNO_ORIGINAL_TEST_RESULTS_DIRECTORY/android-test.binlog -- --results-directory $UNO_ORIGINAL_TEST_RESULTS_DIRECTORY --settings .runsettings @tests.rsp || true
fi

## Dump the emulator's system log
$ANDROID_HOME/platform-tools/adb shell logcat -d > $BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME/android-device-log-$UNO_UITEST_BUCKET_ID-$UITEST_RUNTIME_TEST_GROUP-$UITEST_TEST_MODE_NAME.txt

if [ ! -f "$UNO_ORIGINAL_TEST_RESULTS" ]; then
	echo "ERROR: The test results file $UNO_ORIGINAL_TEST_RESULTS does not exist (did nunit crash ?)"
fi

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $UNO_ORIGINAL_TEST_RESULTS

if [ $? -eq 0 ]; then
	dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST

	if [ "$UITEST_TEST_MODE_NAME" == 'RuntimeTests' ] && [ -f "$UNO_TESTS_FAILED_LIST" ] && [ ! -s "$UNO_TESTS_FAILED_LIST" ]; then
		# Mark successful runtime runs so reruns are skipped when ALLOW_RERUN is enabled.
		echo "invalid-test-for-retry" > "$UNO_TESTS_FAILED_LIST"
	fi
fi

popd
