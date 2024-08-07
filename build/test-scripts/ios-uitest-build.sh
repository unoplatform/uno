﻿#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile

dotnet build -f net8.0-ios17.0 -c Release -p:UnoTargetFrameworkOverride=net8.0-ios17.0 /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/ios-netcoremobile-sampleapp.binlog
