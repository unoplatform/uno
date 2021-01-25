#!/bin/bash
export BUILD_SOURCESDIRECTORY=`pwd`/..
export BUILD_ARTIFACTSTAGINGDIRECTORY=/tmp/uno-uitests-results
export UITEST_SNAPSHOTS_ONLY=true
export UITEST_SNAPSHOTS_GROUP=01
export UNO_UITEST_IOSBUNDLE_PATH="$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/bin/iPhoneSimulator/Release/SamplesApp.app"

mkdir -p $BUILD_ARTIFACTSTAGINGDIRECTORY

pushd $BUILD_SOURCESDIRECTORY
msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj
popd

./ios-uitest-build.sh
./ios-uitest-run.sh
