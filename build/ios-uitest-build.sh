#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY

msbuild /r /p:Configuration=Release $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.iOS/SamplesApp.iOS.csproj
