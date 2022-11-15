#!/bin/bash

pushd $GITPOD_REPO_ROOT/src/SamplesApp/SamplesApp.Skia.Gtk

export NUGET_PACKAGES=/workspace/.nuget

dotnet build /bl SamplesApp.Skia.Gtk.csproj /p:UnoTargetFrameworkOverride=net7.0

popd
