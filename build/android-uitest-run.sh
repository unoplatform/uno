#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'

export BUILDCONFIGURATION=Release

cd $BUILD_SOURCESDIRECTORY/build

# uncomment the following lines to override the installed Xamarin.Android SDK
# wget -nv https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android-d16-2/49/Azure/processDownloadRequest/xamarin-android/xamarin-android/bin/BuildRelease/Xamarin.Android.Sdk-OSS-9.4.0.59_d16-2_6d9b105.pkg
# sudo installer -verbose -pkg Xamarin.Android.Sdk-OSS-9.4.0.59_d16-2_6d9b105.pkg -target /

# Install AVD files
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --install 'system-images;android-28;google_apis;x86'
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --install "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis;x86"

# Create emulator
echo "no" | $ANDROID_HOME/tools/bin/avdmanager create avd -n xamarin_android_emulator -k "system-images;android-$ANDROID_SIMULATOR_APILEVEL;google_apis;x86" --sdcard 128M --force

echo $ANDROID_HOME/emulator/emulator -list-avds

echo "Starting emulator"

# Start emulator in background
nohup $ANDROID_HOME/emulator/emulator -avd xamarin_android_emulator -skin 1280x800 -memory 2048 -no-audio -no-snapshot -netfast -qemu > /dev/null 2>&1 &

export IsUiAutomationMappingEnabled=true

# build the tests, while the emulator is starting
msbuild /r /p:Configuration=$BUILDCONFIGURATION $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj

# Wait for the emulator to finish booting
$BUILD_SOURCESDIRECTORY/build/android-uitest-wait-systemui.sh

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

cd $BUILD_SOURCESDIRECTORY/build

mono nuget/NuGet.exe install NUnit.ConsoleRunner -Version 3.10.0

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

# Move to the screenshot directory so that the output path is the proper one, as
# required by Xamarin.UITest
cd $UNO_UITEST_SCREENSHOT_PATH

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe \
	--inprocess \
	--agents=1 \
	--workers=1 \
	--result=$BUILD_SOURCESDIRECTORY/build/TestResult.xml \
	--where "$TEST_FILTERS" \
	$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/$BUILDCONFIGURATION/net47/SamplesApp.UITests.dll \
	|| true

$ANDROID_HOME/platform-tools/adb shell logcat -d > $BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/$SCREENSHOTS_FOLDERNAME/android-device-log.txt

cp $UNO_UITEST_ANDROIDAPK_PATH $BUILD_ARTIFACTSTAGINGDIRECTORY
