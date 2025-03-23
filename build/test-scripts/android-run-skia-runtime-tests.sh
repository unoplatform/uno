#!/usr/bin/env bash
set -euo pipefail

# echo commands
set -x

export UITEST_IS_LOCAL=${UITEST_IS_LOCAL=false}
export UNO_UITEST_APP_ID="uno.platform.samplesapp.skia"
export UNO_UITEST_ANDROIDAPK_PATH=$BUILD_SOURCESDIRECTORY/build/$SAMPLEAPP_ARTIFACT_NAME/android/$UNO_UITEST_APP_ID-Signed.apk
export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP=automated}
export UNO_ORIGINAL_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml

export LOGS_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/android-skia-$ANDROID_SIMULATOR_APILEVEL-$TARGETPLATFORM_NAME
mkdir -p $LOGS_PATH

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
		-gpu guest \
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

UITEST_RUNTIME_AUTOSTART_RESULT_FILENAME="TestResult-`date +"%Y%m%d%H%M%S"`.xml"
UITEST_RUNTIME_AUTOSTART_RESULT_PATH="/sdcard/$UITEST_RUNTIME_AUTOSTART_RESULT_FILENAME"

# Create the environment file for the app to read
echo "UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}" > samplesapp-environment.txt
echo "UITEST_RUNTIME_TEST_GROUP_COUNT=${UITEST_RUNTIME_TEST_GROUP_COUNT:-}" >> samplesapp-environment.txt
echo "UITEST_RUNTIME_AUTOSTART_RESULT_FILE=$UITEST_RUNTIME_AUTOSTART_RESULT_PATH" >> samplesapp-environment.txt

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
END_TIME=$((SECONDS+TIMEOUT))

echo "Waiting for $UITEST_RUNTIME_AUTOSTART_RESULT_PATH to be available..."

while [[ ! $($ANDROID_HOME/platform-tools/adb shell test -e "$UITEST_RUNTIME_AUTOSTART_RESULT_PATH" > /dev/null) && $SECONDS -lt $END_TIME ]]; do
    sleep 15

    # exit loop if the APP_PID is not running anymore
    if ! $ANDROID_HOME/platform-tools/adb shell ps | grep "$UNO_UITEST_APP_ID" > /dev/null; then
        echo "The app is not running anymore"
        break
    fi
done

$ANDROID_HOME/platform-tools/adb pull $UITEST_RUNTIME_AUTOSTART_RESULT_PATH $UITEST_RUNTIME_AUTOSTART_RESULT_FILENAME

if [ ! -f "$UITEST_RUNTIME_AUTOSTART_RESULT_FILENAME" ]; then
	echo "ERROR: The test results file $UITEST_RUNTIME_AUTOSTART_RESULT_FILENAME does not exist (did nunit crash ?)"
	exit 1
fi

## Dump the emulator's system log
$ANDROID_HOME/platform-tools/adb shell logcat -d > $LOGS_PATH/android-device-log-$UNO_UITEST_BUCKET_ID-$UITEST_RUNTIME_TEST_GROUP-$UITEST_TEST_MODE_NAME.txt
