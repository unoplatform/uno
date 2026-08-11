#!/bin/bash

WWWROOT=$(ls -d $GITPOD_REPO_ROOT/src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/bin/Debug/*/publish/wwwroot | head -1)

python3 -m http.server 8000 -d "$WWWROOT"
