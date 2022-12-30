#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'

export BUILDCONFIGURATION=Release

cd $BUILD_SOURCESDIRECTORY/build

export IsUiAutomationMappingEnabled=true

# build the sample and tests, while the emulator is starting
mono '/Applications/Visual Studio.app/Contents/MonoBundle/MSBuild/Current/bin/MSBuild.dll' /m /r /warnaserror /p:Configuration=$BUILDCONFIGURATION /p:UnoUIUseRoslynSourceGenerators=true /p:AndroidUseLatestPlatformSdk=true $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Droid/SamplesApp.Droid.csproj

mkdir -p $BUILD_ARTIFACTSTAGINGDIRECTORY/android
cp $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Droid/bin/$BUILDCONFIGURATION/uno.platform.unosampleapp-Signed.apk $BUILD_ARTIFACTSTAGINGDIRECTORY/android
