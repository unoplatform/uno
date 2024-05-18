#!/bin/bash

export NUGET_PACKAGES=/workspace/.nuget
dotnet build /bl src/Uno.UI-Skia-only.slnf /p:UnoTargetFrameworkOverride=net8.0 /p:EnableWindowsTargeting=true

popd
