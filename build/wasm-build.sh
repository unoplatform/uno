#!/bin/bash

pushd src/SamplesApp/SamplesApp.Wasm

msbuild /r /bl SamplesApp.Wasm.csproj /p:UnoSourceGeneratorUseGenerationHost=true /p:UnoSourceGeneratorUseGenerationController=false /p:UnoRemoteControlPort=443

popd
