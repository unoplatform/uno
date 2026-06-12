#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Skia.netcoremobile

dotnet build -f net10.0-ios -c Release -p:UnoTargetFrameworkOverride=net10.0-ios -p:UNO_DISABLE_ANALYZERS_IN_SAMPLES=true /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/skia-ios-netcoremobile-sampleapp.binlog
