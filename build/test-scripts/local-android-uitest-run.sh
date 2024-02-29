#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

export BUILD_SOURCESDIRECTORY=`pwd`/../..
export BUILD_ARTIFACTSTAGINGDIRECTORY=/tmp/uno-uitests-results
export UITEST_SNAPSHOTS_ONLY=false
export UITEST_SNAPSHOTS_GROUP=01
export UITEST_TEST_MODE_NAME=RuntimeTests
export UITEST_RUNTIME_TEST_GROUP_COUNT=8
export UITEST_RUNTIME_TEST_GROUP=5
export UNO_UITEST_BUCKET_ID=0
export UITEST_TEST_TIMEOUT=10m
export TARGETPLATFORM_NAME=net7
export UITEST_IGNORE_RERUN_FILE=true
export ANDROID_SIMULATOR_APILEVEL=28
export SAMPLEAPP_ARTIFACT_NAME=android-netcoremobile-sampleapp
export UITEST_IS_LOCAL=true

mkdir -p $BUILD_ARTIFACTSTAGINGDIRECTORY

export UnoDisableNetCurrentMobile=true
export UnoDisableNetCurrent=true

# Comment out the following line to avoid full rebuild for subsequent runs
# pushd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile
# dotnet publish -f net7.0-android -c Release -p:UnoTargetFrameworkOverride=net7.0-android /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/android-netcoremobile-sampleapp.binlog
# popd

mkdir -p $BUILD_SOURCESDIRECTORY/build/$SAMPLEAPP_ARTIFACT_NAME/android
cp -f $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile/bin/Release/net7.0-android/uno.platform.unosampleapp-Signed.apk $BUILD_SOURCESDIRECTORY/build/$SAMPLEAPP_ARTIFACT_NAME/android/uno.platform.unosampleapp-Signed.apk

rm -fR ~/.android/avd/xamarin_android_emulator.avd

./android-uitest-run.sh
