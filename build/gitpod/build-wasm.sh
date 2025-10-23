#!/bin/bash

export NUGET_PACKAGES=/workspace/.nuget

GITPOD_HOSTNAME=`echo $GITPOD_WORKSPACE_URL | sed -s 's/https:\/\///g'`

dotnet build /bl src/Uno.UI-Wasm-only.slnf /p:UnoTargetFrameworkOverride=net9.0 /p:EnableWindowsTargeting=true /p:UnoRemoteControlPort=443 "/p:UnoRemoteControlHost=53487-$GITPOD_HOSTNAME"
