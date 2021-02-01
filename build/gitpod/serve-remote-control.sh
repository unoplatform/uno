#!/bin/bash

pushd $GITPOD_REPO_ROOT/src/Uno.UI.RemoteControl.Host

dotnet run --httpPort 53487

popd
