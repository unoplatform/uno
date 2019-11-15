#!/bin/bash

pushd /workspace/uno/src/SamplesApp/SamplesApp.Wasm

dotnet unowasm serve --urls=http://*:8000 --pathbase /workspace/uno/src/SamplesApp/SamplesApp.Wasm/bin/Debug/netstandard2.0/dist

popd
