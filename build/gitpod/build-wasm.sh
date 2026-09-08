#!/bin/bash

export NUGET_PACKAGES=/workspace/.nuget

GITPOD_HOSTNAME=`echo $GITPOD_WORKSPACE_URL | sed -s 's/https:\/\///g'`

# The browser target runs on Skia; publishing the head produces the wwwroot that
# serve-sampleapp-wasm.sh serves. The head is multi-targeted, so the TFM is explicit
# (tracks $(NetCurrent) in Directory.Build.props).
_TFM="${TFM:=net11.0-browserwasm}"

dotnet publish /bl src/SamplesApp/SamplesApp/SamplesApp.csproj -c Debug -f "$_TFM" "/p:UnoTargetFrameworkOverride=$_TFM" /p:EnableWindowsTargeting=true /p:UnoRemoteControlPort=443 "/p:UnoRemoteControlHost=53487-$GITPOD_HOSTNAME"
