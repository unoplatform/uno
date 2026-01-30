#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile

dotnet build -f net9.0-ios -c Release -p:UnoTargetFrameworkOverride=net9.0-ios -p:ValidateXcodeVersion=false /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/logs/ios-netcoremobile-sampleapp.binlog
