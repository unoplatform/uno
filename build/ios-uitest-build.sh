#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY

mono '/Applications/Visual Studio.app/Contents/Resources/lib/monodevelop/bin/MSBuild/Current/bin/msbuild.dll' /m /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/SamplesApp.iOS.csproj
