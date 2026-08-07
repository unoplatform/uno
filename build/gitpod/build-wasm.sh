#!/bin/bash

export NUGET_PACKAGES=/workspace/.nuget

GITPOD_HOSTNAME=`echo $GITPOD_WORKSPACE_URL | sed -s 's/https:\/\///g'`

# The browser target runs on Skia; publishing the head produces the wwwroot that
# serve-sampleapp-wasm.sh serves.
dotnet publish /bl src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/SamplesApp.Skia.WebAssembly.Browser.csproj -c Debug /p:EnableWindowsTargeting=true /p:UnoRemoteControlPort=443 "/p:UnoRemoteControlHost=53487-$GITPOD_HOSTNAME"
