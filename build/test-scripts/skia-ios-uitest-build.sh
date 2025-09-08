#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Skia.netcoremobile

dotnet build -f net9.0-ios18.5 -c Release -p:UnoTargetFrameworkOverride=net9.0-ios18.5 /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/skia-ios-netcoremobile-sampleapp.binlog
