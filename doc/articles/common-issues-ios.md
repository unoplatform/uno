---
uid: Uno.UI.CommonIssues.Ios
---

# Issues related to iOS projects

## Developing on older Mac hardware

The latest macOS release and Xcode version are required to develop with Uno Platform for iOS. However, if you have an older Mac that does not support the latest macOS release, you can use a third-party tool to upgrade it, such as [OpenCore Legacy Patcher](https://dortania.github.io/OpenCore-Legacy-Patcher/). While not ideal, this can extend the use of older hardware by installing the latest macOS release on it. Please note that this method is not required when developing for other targets such as Android, Skia, WebAssembly, or Windows.

## `Don't know how to marshal a return value of type 'System.IntPtr'`

[This issue](https://github.com/unoplatform/uno/issues/9430) may happen for Uno.UI 4.4.20 and later, when deploying an application using the iOS Simulator, when the application contains a `TextBox`.

In order to fix this, add the following to your `.csproj`:

```xml
<PropertyGroup>
  <MtouchExtraArgs>$(MtouchExtraArgs) --registrar=static</MtouchExtraArgs>
</PropertyGroup>
```

## Error while retrieving iOS device in VS code

When switching to an iOS debugging target in VS Code, you might encounter an error stating that the iOS device could not be retrieved. The error message may appear as follows:

```error
[Info]: Project reload forced to switch to net8.0-ios | Debug
[Error] Could not retrieve ios devices within 10 seconds. Aborting...
```

To resolve this issue, download [Xcodes](https://www.xcodes.app). Inside Xcodes.app, select the correct version of Xcode and click the **Make Active** button to make it the default Xcode for your Mac. After completing this step, you can speed up the process and use the new default Xcode for simulators. On VS Code, open the Command Palette and select `Developer: Reload Window`. This should resolve the error when switching to an iOS debugging target in VS Code.

## Build stops with `Verification of iOS environment is running. Please try again in a moment`

When building for an iOS physical device, the following error may happen in your build `Verification of iOS environment is running. Please try again in a moment.`. If this happens and your Visual Studio is connected to your Mac, you may need to ensure that you have selected a provisioning profile.

Make sure to [configure your Apple account](https://learn.microsoft.com/en-us/dotnet/maui/ios/device-provisioning/automatic-provisioning?view=net-maui-9.0#enable-automatic-provisioning), or in some cases, selecting the development team and provisioning profile is required.

## Debugging takes a long time when connecting from a Windows machine

In case your debugging experience is slow when connecting from a Windows VS environment to a Mac machine, make sure that you're not connected through Wifi on either end. Try pinging your Mac from your Windows machine and ensure it's lower than 5ms.

## Build fails with `The "AOTCompile" task failed unexpectedly`

After a .NET workload update (for example `dotnet workload update` or `uno-check --fix`), an iOS build can fail with an error similar to:

```error
error MSB4018: The "AOTCompile" task failed unexpectedly.
An error occurred trying to start process '/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Runtime.AOT.osx-arm64.Cross.iossimulator-arm64/10.0.8/Sdk/../tools/mono-aot-cross'. No such file or directory
```

The build reuses the AOT compiler path saved by an earlier build in the `obj` folder, and doesn't refresh it when only the workloads change ([dotnet/macios#25481](https://github.com/dotnet/macios/issues/25481)). The version in the path is the old compiler, which the update removed.

To fix it, delete the project's `bin` and `obj` folders and build again. Deleting only the `aot-compiler-path-*.txt` file is enough, for example `obj/Debug/net10.0-ios/iossimulator-arm64/aot-compiler-path-10.0.401.txt`. Don't install the older AOT compiler to match the path: the app then fails to link, because that compiler doesn't match the runtime.

## Additional troubleshooting

You can get additional build [troubleshooting information here](uno-builds-troubleshooting.md).
