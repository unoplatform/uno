#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY

mono '/Applications/Visual Studio.app/Contents/MonoBundle/MSBuild/Current/bin/MSBuild.dll' /m /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/SamplesApp.iOS.csproj /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/ios-uitest-build.binlog
