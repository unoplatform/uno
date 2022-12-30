#!/bin/bash
export BUILD_SOURCESDIRECTORY=`pwd`/../..
export BUILD_ARTIFACTSTAGINGDIRECTORY=/tmp/uno-uitests-results
export UITEST_SNAPSHOTS_ONLY=false
export UITEST_SNAPSHOTS_GROUP=01
export UITEST_AUTOMATED_GROUP=Local
export UITEST_TEST_TIMEOUT=120000
export UITEST_IGNORE_RERUN_FILE=true
export UNO_UITEST_IOSBUNDLE_PATH="$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/bin/iPhoneSimulator/Release/SamplesApp.app"

mkdir -p $BUILD_ARTIFACTSTAGINGDIRECTORY

pushd $BUILD_SOURCESDIRECTORY
msbuild /m /r /warnaserror /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj
popd

# Comment out the following line to avoid full rebuild for subsequent runs
./ios-uitest-build.sh
./ios-uitest-run.sh
