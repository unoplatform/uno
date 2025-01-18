#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Skia.netcoremobile

dotnet build -f net8.0-ios -c Release -p:UnoTargetFrameworkOverride=net8.0-ios /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/skia-ios-netcoremobile-sampleapp.binlog
