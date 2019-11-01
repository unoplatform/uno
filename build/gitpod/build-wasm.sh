#!/bin/bash

pushd src/SamplesApp/SamplesApp.Wasm

GITPOD_HOSTNAME=`echo $GITPOD_WORKSPACE_URL | sed -s 's/https:\/\///g'`

msbuild /r /bl SamplesApp.Wasm.csproj /p:UnoSourceGeneratorUseGenerationHost=true /p:UnoSourceGeneratorUseGenerationController=false /p:UnoRemoteControlPort=443 "/p:UnoRemoteControlHost=https://53487-$GITPOD_HOSTNAME"

popd
