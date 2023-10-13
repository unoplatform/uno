#!/bin/bash

pushd $GITPOD_REPO_ROOT/src/SamplesApp/SamplesApp.Wasm

export NUGET_PACKAGES=/workspace/.nuget

GITPOD_HOSTNAME=`echo $GITPOD_WORKSPACE_URL | sed -s 's/https:\/\///g'`

msbuild /r /bl SamplesApp.Wasm.csproj /p:UnoTargetFrameworkOverride=net7.0 /p:UnoRemoteControlPort=443 "/p:UnoRemoteControlHost=53487-$GITPOD_HOSTNAME"

popd
