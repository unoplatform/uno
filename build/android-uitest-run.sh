#!/usr/bin/env bash

# Install AVD files
echo "y" | $ANDROID_HOME/tools/bin/sdkmanager --install 'system-images;android-28;google_apis;x86'

# Create emulator
echo "no" | $ANDROID_HOME/tools/bin/avdmanager create avd -n xamarin_android_emulator -k 'system-images;android-28;google_apis;x86' --sdcard 128M --force

echo $ANDROID_HOME/emulator/emulator -list-avds

echo "Starting emulator"

# Start emulator in background
nohup $ANDROID_HOME/emulator/emulator -avd xamarin_android_emulator -skin 1280x800 -memory 2048 -no-audio -no-snapshot -netfast > /dev/null 2>&1 &

export IsUiAutomationMappingEnabled=true

# build the sample and tests, while the emulator is starting
msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Droid/SamplesApp.Droid.csproj
msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj

# Wait for the emulator to finish booting
chmod +x $BUILD_SOURCESDIRECTORY/build/android-uitest-wait-systemui.sh
$BUILD_SOURCESDIRECTORY/build/android-uitest-wait-systemui.sh

$ANDROID_HOME/platform-tools/adb devices

echo "Emulator started"

export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/android
export UNO_UITEST_PLATFORM=Android
export UNO_UITEST_ANDROIDAPK_PATH=$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Droid/bin/Release/uno.platform.unosampleapp.apk

cd $BUILD_SOURCESDIRECTORY/build

mono nuget/NuGet.exe install NUnit.ConsoleRunner -Version 3.10.0

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/bin/Release/net47/SamplesApp.UITests.dll

$ANDROID_HOME/platform-tools/adb shell logcat -d > $BUILD_ARTIFACTSTAGINGDIRECTORY/android-device-log.txt
