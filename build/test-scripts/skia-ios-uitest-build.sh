#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

_TFM="${TFM:=net10.0-ios}"

cd $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.Head

dotnet build -f "$_TFM" -c Release "-p:UnoTargetFrameworkOverride=$_TFM" -p:UNO_DISABLE_ANALYZERS_IN_SAMPLES=true /bl:$BUILD_ARTIFACTSTAGINGDIRECTORY/skia-ios-netcoremobile-sampleapp.binlog
