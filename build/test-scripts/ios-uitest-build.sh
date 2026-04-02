#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile

dotnet build -f net10.0-ios26.0 -c Release -p:UnoTargetFrameworkOverride=net10.0-ios26.0 /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/logs/ios-netcoremobile-sampleapp.binlog
