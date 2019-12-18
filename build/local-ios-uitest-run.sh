#!/bin/bash
export BUILD_SOURCESDIRECTORY=`pwd`/..
export BUILD_ARTIFACTSTAGINGDIRECTORY=/tmp/uno-uitests-results
export UITEST_SNAPSHOTS_ONLY=true
export UITEST_SNAPSHOTS_GROUP=01
export UNO_UITEST_IOSBUNDLE_PATH="$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/bin/iPhoneSimulator/Release/SamplesApp.app"

mkdir -p $BUILD_ARTIFACTSTAGINGDIRECTORY

./ios-uitest-build.sh
./ios-uitest-run.sh
