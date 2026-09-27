#!/bin/bash

WWWROOT=$(ls -d $GITPOD_REPO_ROOT/src/SamplesApp/SamplesApp/bin/Debug/*-browserwasm/publish/wwwroot | head -1)

python3 -m http.server 8000 -d "$WWWROOT"
