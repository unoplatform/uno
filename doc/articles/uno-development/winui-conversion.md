---
uid: Uno.Contributing.WinUIConversion
---

# Converting the source tree to WinUI

The current Uno source tree is based on UWP, and the CI uses a specific step to generate the WinUI 3.0 compatible API set, and the associated packages.

## Using the auto-generated synchronized branch
<<<<<<< HEAD:doc/articles/uno-development/winui-conversion.md
The Uno CI automatically maintains a WinUI-converted branch of the tree after every push to tracked branches, in order to get started faster when debugging WinUI related issues.
=======

The Uno CI automatically maintains a UWP-converted branch of the tree after every push to tracked branches, in order to get started faster when debugging UWP related issues.
>>>>>>> 7ee7368c0b (chore: Auto-fix):doc/articles/uno-development/uwp-conversion.md

The branch is maintained as follows:

```
generated\[branch]\winui-autoconvert
```

You can checkout this branch locally to get started faster.

## The conversion process

The conversion process is done as follows, from a clean repository:
<<<<<<< HEAD:doc/articles/uno-development/winui-conversion.md
- The `Uno.WinUIRevert` is removing and moving folders from the UWP structure to adjust to the WinUI structure
- The `Uno.UWPSyncGenerator` is run to regenerate the whole WinRT/WinUI 3.0 API set
- A set of nuspec conversions are performed in `build\Uno.UI.Build.csproj` in the `BuildNuGetPackage` target

To ease the adjustments when conversion issues arise:
- The `UNO_UWP_BUILD` msbuild variable is set to `true` when the tree is "UWP" mode, and undefined when the tree is in WinUI mode.
- The `HAS_UNO_WINUI` C# constant is defined when the tree is built in WinUI mode.
=======

- The `Uno.WinUIRevert` is removing and moving folders from the WinUI structure to adjust to the UWP structure
- The `Uno.WinUISyncGenerator` is run to regenerate the whole WinRT/UWP 3.0 API set
- A set of nuspec conversions are performed in `build\Uno.UI.Build.csproj` in the `BuildNuGetPackage` target

To ease the adjustments when conversion issues arise:

- The `UNO_UWP_BUILD` msbuild variable is set to `true` when the tree is "UWP" mode, and undefined when the tree is in UWP mode.
- The `HAS_UNO_UWP` C# constant is defined when the tree is built in UWP mode.
>>>>>>> 7ee7368c0b (chore: Auto-fix):doc/articles/uno-development/uwp-conversion.md

## Converting a local source tree

The conversion process can be run locally as follows:
<<<<<<< HEAD:doc/articles/uno-development/winui-conversion.md
- cd `uno-repo\build\scripts`
- `convert-sourcetree-to-winui.cmd`
=======

- cd `uno-repo\build`
- `convert-sourcetree-to-uwp.cmd`
>>>>>>> 7ee7368c0b (chore: Auto-fix):doc/articles/uno-development/uwp-conversion.md

You'll need to ensure that the `crosstargeting_override.props` file is not defining `UnoTargetFrameworkOverride` otherwise the UWPSyncGenerator will generate an invalid API set.
