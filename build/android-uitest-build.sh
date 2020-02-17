#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'

export BUILDCONFIGURATION=Release

cd $BUILD_SOURCESDIRECTORY/build

export IsUiAutomationMappingEnabled=true

# build the sample and tests, while the emulator is starting
msbuild /r /p:Configuration=$BUILDCONFIGURATION $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Droid/SamplesApp.Droid.csproj

mkdir -p $BUILD_ARTIFACTSTAGINGDIRECTORY/android
cp $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Droid/bin/$BUILDCONFIGURATION/uno.platform.unosampleapp-Signed.apk $BUILD_ARTIFACTSTAGINGDIRECTORY/android
