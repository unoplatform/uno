#!/bin/bash

export NUGET_PACKAGES=/workspace/.nuget
dotnet build /bl src/Uno.UI-Skia-only.slnf /p:UnoTargetFrameworkOverride=net9.0 /p:EnableWindowsTargeting=true

popd
