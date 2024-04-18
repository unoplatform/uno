#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile

dotnet build -f net8.0-ios -r iossimulator-x64 -c Release -p:UnoTargetFrameworkOverride=net8.0-ios /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/ios-netcoremobile-sampleapp.binlog
