#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'

export BUILDCONFIGURATION=Release
export ANDROID_HOME=$BUILD_SOURCESDIRECTORY/build/android-sdk

cd $BUILD_SOURCESDIRECTORY/build

mkdir android-sdk
pushd android-sdk
# URL from https://developer.android.com/studio/index.html#command-tools
wget https://dl.google.com/android/repository/commandlinetools-mac-6200805_latest.zip
unzip commandlinetools-mac-6200805_latest.zip
popd

# uncomment the following lines to override the installed Xamarin.Android SDK
# wget -nv https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android-d16-2/49/Azure/processDownloadRequest/xamarin-android/xamarin-android/bin/BuildRelease/Xamarin.Android.Sdk-OSS-9.4.0.59_d16-2_6d9b105.pkg
# sudo installer -verbose -pkg Xamarin.Android.Sdk-OSS-9.4.0.59_d16-2_6d9b105.pkg -target /

# Install AVD files
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'tools'| tr '\r' '\n' | uniq
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'platform-tools'  | tr '\r' '\n' | uniq
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'build-tools;28.0.3' | tr '\r' '\n' | uniq
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'platforms;android-28' | tr '\r' '\n' | uniq
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'extras;android;m2repository' | tr '\r' '\n' | uniq
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install 'system-images;android-28;google_apis_playstore;x86' | tr '\r' '\n' | uniq
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --sdk_root=${ANDROID_HOME} --install "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis_playstore;x86" | tr '\r' '\n' | uniq

if [[ -f $ANDROID_HOME/platform-tools/platform-tools/adb ]]
then
	# It appears that the platform-tools 29.0.6 are extracting into an incorrect path
    mv $ANDROID_HOME/platform-tools/platform-tools/* $ANDROID_HOME/platform-tools
fi

# Create emulator
echo "no" | $ANDROID_HOME/tools/bin/avdmanager create avd -n xamarin_android_emulator -k "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis_playstore;x86" --sdcard 128M --force

echo $ANDROID_HOME/emulator/emulator -list-avds

echo "Checking for hardware acceleration"
$ANDROID_HOME/emulator/emulator -accel-check

echo "Starting emulator"

# Start emulator in background
nohup $ANDROID_HOME/emulator/emulator -avd xamarin_android_emulator -skin 1280x800 -memory 2048 -no-audio -no-snapshot -netfast -qemu > /dev/null 2>&1 &

export IsUiAutomationMappingEnabled=true

# build the tests, while the emulator is starting
msbuild /r /p:Configuration=$BUILDCONFIGURATION $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj

# Wait for the emulator to finish booting
$BUILD_SOURCESDIRECTORY/build/android-uitest-wait-systemui.sh

# Restart the emulator to avoid running first-time tasks
$ANDROID_HOME/platform-tools/adb reboot

# Wait for the emulator to finish booting
$BUILD_SOURCESDIRECTORY/build/android-uitest-wait-systemui.sh

# list active devices
$ANDROID_HOME/platform-tools/adb devices

echo "Emulator started"

if [ "$UITEST_SNAPSHOTS_ONLY" == 'true' ];
then
	export TEST_FILTERS="namespace == 'SamplesApp.UITests.Snap'"
	export SCREENSHOTS_FOLDERNAME=android-$ANDROID_SIMULATOR_APILEVEL-Snap
else
	export TEST_FILTERS="namespace != 'SamplesApp.UITests.Snap'"
	export SCREENSHOTS_FOLDERNAME=android-$ANDROID_SIMULATOR_APILEVEL
fi

export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME
export UNO_UITEST_PLATFORM=Android
export UNO_UITEST_ANDROIDAPK_PATH=$BUILD_SOURCESDIRECTORY/build/uitests-android-build/android/uno.platform.unosampleapp-Signed.apk

export UNO_ORIGINAL_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml
export UNO_RERUN_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-failed-rerun.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/failed-tests.txt

cp $UNO_UITEST_ANDROIDAPK_PATH $BUILD_ARTIFACTSTAGINGDIRECTORY

cd $BUILD_SOURCESDIRECTORY/build

export NUNIT_VERSION=3.11.1
mono nuget/NuGet.exe install NUnit.ConsoleRunner -Version $NUNIT_VERSION

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

# Move to the screenshot directory so that the output path is the proper one, as
# required by Xamarin.UITest
cd $UNO_UITEST_SCREENSHOT_PATH

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.$NUNIT_VERSION/tools/nunit3-console.exe \
	--trace=Verbose \
	--framework=mono \
	--inprocess \
	--agents=1 \
	--workers=1 \
	--result=$UNO_ORIGINAL_TEST_RESULTS \
	--timeout=120000 \
	--where "$TEST_FILTERS" \
	$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/$BUILDCONFIGURATION/net47/SamplesApp.UITests.dll \
	|| true

$ANDROID_HOME/platform-tools/adb shell logcat -d > $BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME/android-device-log.1.txt

pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST
popd

if [ -n "`cat $UNO_TESTS_FAILED_LIST`" ]; then
	# Rerun failed tests
	echo Retrying failed tests

	# Restart the emulator to avoid lingering emulator state isses
	$ANDROID_HOME/platform-tools/adb reboot

	# Wait for the emulator to finish booting
	$BUILD_SOURCESDIRECTORY/build/android-uitest-wait-systemui.sh

	mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.$NUNIT_VERSION/tools/nunit3-console.exe \
		--trace=Verbose \
		--framework=mono \
		--inprocess \
		--agents=1 \
		--workers=1 \
		--result=$UNO_RERUN_TEST_RESULTS \
		--timeout=300000 \
		--testlist $UNO_TESTS_FAILED_LIST \
		$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/$BUILDCONFIGURATION/net47/SamplesApp.UITests.dll \
		|| true
fi

$ANDROID_HOME/platform-tools/adb shell logcat -d > $BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME/android-device-log.2.txt

