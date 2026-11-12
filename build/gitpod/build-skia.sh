#!/bin/bash

export NUGET_PACKAGES=/workspace/.nuget
dotnet build /bl src/Uno.UI-Uno-only.slnf /p:UnoTargetFrameworkOverride=net10.0 /p:EnableWindowsTargeting=true

popd
