---
uid: Uno.Contributing.UWPConversion
---

# Converting the source tree to UWP

The current Uno Platform source tree is based on WinUI, and the CI uses a specific step to generate the UWP compatible API set and the associated packages.

## Using the auto-generated synchronized branch

The Uno CI automatically maintains a UWP-converted branch of the tree after every push to tracked branches, in order to get started faster when debugging UWP-related issues. The branch is maintained as `generated\[branch]\uwp-autoconvert`.

You can checkout this branch locally to get started faster.

## The conversion process

The conversion process is done as follows, from a clean repository:

- The `Uno.WinUIRevert` is removing and moving folders from the WinUI structure to adjust to the UWP structure
- The `Uno.UWPSyncGenerator` is run to regenerate the whole WinRT/UWP API set
- A set of nuspec conversions are performed in `build\Uno.UI.Build.csproj` in the `BuildNuGetPackage` target

To ease the adjustments when conversion issues arise:

- The `UNO_UWP_BUILD` MSBuild property is set to `true` when the tree is "UWP" mode, and set to `false` when the tree is in WinUI mode.
- The `HAS_UNO_WinUI` C# constant is defined when the tree is built in WinUI mode.

## Converting a local source tree

The conversion process can be run locally as follows:

- cd `uno-repo\build`
- `convert-sourcetree-to-uwp.cmd`

You'll need to ensure that the `crosstargeting_override.props` file is not defining `UnoTargetFrameworkOverride` otherwise the UWPSyncGenerator will generate an invalid API set.
