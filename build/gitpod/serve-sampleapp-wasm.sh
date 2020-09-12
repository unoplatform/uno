#!/bin/bash

pushd $GITPOD_REPO_ROOT/src/SamplesApp/SamplesApp.Wasm

dotnet $NUGET_PACKAGES/uno.wasm.bootstrap.devserver/*/tools/server/dotnet-unowasm.dll serve --urls=http://*:8000 --pathbase $GITPOD_REPO_ROOT/src/SamplesApp/SamplesApp.Wasm/bin/Debug/netstandard2.0/dist

popd
