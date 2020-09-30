# Converting the source tree to WinUI

The current Uno source tree is based on UWP, and the CI uses a specific step to generate the WinUI 3.0 compatible API set, and the associated packages.

The conversion process is done as follows, from a clean repository:
- The `Uno.WinUIRevert` is removing and moving folders from the UWP structure to adjust to the WinUI structure
- The `Uno.UWPSyncGenerator` is run to regenerate the whole WinRT/WinUI 3.0 API set
- A set of nuspec conversions are performed in `build\Uno.UI.Build.csproj` in the `BuildNuGetPackage` target

To ease the adjustments when conversion issues arise:
- The `UNO_UWP_BUILD` msbuild variable is set to `true` when the tree is "UWP" mode, and undefined when the tree is in WinUI mode.
- The `HAS_UNO_WINUI` C# constant is defined when the tree is built in WinUI mode.

The conversion process can be run locally as follows:
- cd `uno-repo\build`
- `convert-sourcetree-to-winui.cmd`

You'll need to ensure that the `crosstargeting_override.props` file is not defining `UnoTargetFrameworkOverride` otherwise the UWPSyncGenerator will generate an invalid API set.
