---
uid: uno.vscode.mobile.advanced.debugging
---

# Advanced Topics for VS Code for Mobile Targets

Starting from Uno 4.8.26, the Uno Platform solution templates contains the appropriate support to debug Mobile applications. See below for adjusting projects.

## Supported Features

### SourceLink and Sources embedded inside PDB

Both [Source Link](https://github.com/dotnet/designs/blob/main/accepted/2020/diagnostics/source-link.md) and "Sources embedded inside PDB" features are used by Uno Platform and are supported by the extension.

However, only Android includes the `.pdb` of the referenced assemblies inside debug builds for net6.0. This makes the features unusable for iOS and macOS, see [issue](https://github.com/xamarin/xamarin-macios/issues/11879#issuecomment-1279452073).

This situation [should be](https://github.com/dotnet/sdk/issues/1458#issuecomment-1244736464) fixed with net7.0. A workaround for .NET 6 mobile projects is to install [Cymbal](https://github.com/SimonCropp/Cymbal).

### Remote SSH

VS Code can [connect to a remote computer](https://code.visualstudio.com/docs/remote/ssh) (using `ssh`) to develop and debug projects. Note that all the sources, build and debugging are done on the remote computer, where the Uno Platform extension is being executed.

### Logpoints

[Logpoints](https://code.visualstudio.com/blogs/2018/07/12/introducing-logpoints-and-auto-attach#_introducing-logpoints) are functionally similar to adding `Console.WriteLine` but without modifying the sources.

## Converting existing projects

Existing Uno Platform projects requires a few simple changes to work properly with multiples TargetFrameworks inside VS Code.

### launch.json

You can add a default entry for all mobile targets by adding the following JSON block inside your `launch.json`.

```json
{
  "name": "Uno Platform Mobile",
  "type": "Uno",
  "request": "launch",
  // any Uno* task will do, this is simply to satisfy vscode requirement when a launch.json is present
  "preLaunchTask": "Uno: net7.0-android | Debug | android-x64"
},
```

This will ask the `Uno` debug provider how to build and launch the application. The Target Framework Moniker (TFM) selected in VS Code's status bar, e.g. `net7.0-ios`, and the target platform, e.g. `iossimulator-x64`, will be used automatically.

> [!NOTE]
> Uno Platform's [templates](https://www.nuget.org/packages/uno.templates/) already include this change.

## Debugging with the Windows Subsystem for Android

You first need to [connect](https://learn.microsoft.com/windows/android/wsa/#connect-to-the-windows-subsystem-for-android-for-debugging) to the Windows Subsystem for Android (WSA) in order to use it for debugging. From the command-line type:

```bash
adb connect 127.0.0.1:58526
```

and you should then be able to see the WSA device inside VSCode. You can also confirm it's connected from the command-line:

```bash
$ adb devices -l
List of devices attached
127.0.0.1:58526        device product:windows_arm64 model:Subsystem_for_Android_TM_ device:windows_arm64 transport_id:1
```

## Settings

### Unoplatform › Debugger: Exception Options

You can choose to break execution on specific exceptions, either always or if the exception is unhandled.

Note: Those settings do not apply when debugging Windows (WinUI) or macOS (AppKit) application since it use the CoreCLR debugger.

### Unoplatform › Debugger › Ios › Mlaunch: Extra Arguments

For example you can add `-vvvvv` to get more verbosity from `mlaunch`.

This can be useful if you run into issues deploying your application to an iOS device.

### Unoplatform › Debugger › Ios › Mlaunch: Path

By default the version of `mlaunch` associated with the currently iOS workload will be used to launch application to iOS devices.

You can override the path to the `mlaunch` tool to be used. For example you could set the path to `/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch` to use the version from the legacy (pre-net6.0) Xamarin.iOS SDK.

This can be useful when a new Xcode is released, requiring an update to `mlaunch`, while allowing you to use the current stable SDK version for development.

### Unoplatform › Debugger › Ios › Simulator: X64

By default the simulators are listed for both `arm64` and `x64` on Apple Silicon Macs.
`x64` simulators are provided using Rosetta emulation and is required if `arm64` support is missing for some dependencies (e.g. SkiaSharp).

This can be useful if your project has dependencies that are not available yet for the `arm64` architecture.

## Environment Variables

### Xcode override

The Uno extension uses `xcrun` to find the active Xcode version to use. You can override this version of Xcode by setting the `DEVELOPER_DIR` environment variable. More information can be found running `man xcrun` inside the terminal.

## Advanced Debugger Configuration

It is possible to create advanced `launch.json` and `tasks.json` entries if additional customization is needed for a project.

### Editing `launch.json`

The extension provides default launch configurations for supported platforms, without the need to define them inside the `launch.json` file.

If you need to define launches or modify the defaults then you can can create your own.

1. Press the `Shift+Cmd+P` keys to open the **Command Palette**
1. Select the `Debug: Select and Start Debugging` item
1. Select the `Add Configuration...` item
1. Select `.NET Custom Launch Configuration for Uno Platform` item
1. This will insert a JSON block that looks like the following:

    ```json
    {
      "comment1": "// name: unique name for the configuration, can be identical to preLaunchTask",
      "name": "Uno: net6.0-android | Debug | android-x64",
      "comment2": "// type: 'Uno' for mono-based SDK, 'coreclr' for desktop targets",
      "type": "Uno",
      "request": "launch",
      "comment3": "// preLaunchTask format is 'Uno: {tfm} | {config}[ | {rid]}]' where ",
      "comment4": "// * {tfm} is the target framework moniker, e.g. net6.0-ios",
      "comment5": "// * {config} is the build configuration, e.g. 'Debug' or 'Release'",
      "comment6": "// * {rid} is the optional runtime identifier, e.g. 'osx-arm64'",
      "comment7": "// E.g. 'Uno: net6.0-ios | Debug | iossimulator-x64', 'Uno: net6.0 | Debug' for desktop",
      "preLaunchTask": "Uno: net6.0-android | Debug | android-x64"
    },
    ```

1. Follow the comments to construct the launch required by your target platform, for example:

    ```json
    {
      "name": "net7.0-ios | simulator | x64",
      "type": "Uno",
      "request": "launch",
      "preLaunchTask": "Uno: net7.0-ios | Debug | iossimulator-x64"
    },
    ```

If you follow the `Uno: {tfm} | {config}[ | {rid]}]` convention then there is no need to add entries inside the `tasks.json` file, the extension already provides them.

### Customizing tasks.json

The extension provides default build tasks for most platforms without the need to define them inside the `tasks.json` file.
If you need to define tasks or something that the default built-in tasks do not provide, you can create your own.

1. Press the `Shift+Cmd+P` keys to open the **Command Palette**.
1. Select the `Tasks: Configure Tasks` item.
1. Pick the `Uno: *` task that is the closest to your need.
1. Edit the `args` section but avoid changing the other values (besides the `label`) to keep automation working.

See [VS Code documentation](https://code.visualstudio.com/docs/editor/tasks) for more information.

#### Example

```json
{
  "label": "custom-mac-build",
  "command": "dotnet",
  "type": "process",
  "args": [
    "build",
    "${workspaceFolder}/unoapp/unoapp.csproj",
    "/property:GenerateFullPaths=true",
    "/consoleloggerparameters:NoSummary",
    // specify the target platform - since there's more than one inside the mobile.csproj
    "/property:Configuration=Debug",
    // this is to workaround both an OmniSharp limitation and a dotnet issue #21877
    "/property:UnoForceSingleTFM=true"
    // other custom settings that you need
  ],
  "problemMatcher": "$msCompile"
},
```

#### Tips

* Most of the build settings should be done (or changed) inside the `.csproj` file. This way only a simple `dotnet build` is needed to create the applications.

* The easiest way to create a custom task is to look at the build logs and see what is normally provided, by default, to build for a specific target platform.

* You will also need a custom launch target that will use the `label` name for it's `preLaunchTask`.
