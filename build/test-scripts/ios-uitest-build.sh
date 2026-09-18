#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

_TFM="${TFM:=net10.0-ios}"

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.netcoremobile

dotnet build -f "$_TFM" -c Release "-p:UnoTargetFrameworkOverride=$_TFM" /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/logs/ios-netcoremobile-sampleapp.binlog
