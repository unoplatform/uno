#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile

dotnet build -f net7.0-ios -r iossimulator-x64 -c Release -p:UnoTargetFrameworkOverride=net7.0-ios -p:UnoUIDisableNet8Build=true /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/ios-netcoremobile-sampleapp.binlog
