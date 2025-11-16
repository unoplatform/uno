---
uid: Uno.Contributing.BuildingUno
---

# Building Uno.UI

This article explains how to build Uno.UI locally, for instance, if you wish to contribute a bugfix or new feature.

## Prerequisites

- Visual Studio 2022 (17.8.1 or later)
  - Select the **ASP.NET and Web Development** workload
  - Select the **.NET Multi-Platform App UI development** workload
  - Select the **.NET desktop development** workload
  - To build the UWP flavor of Uno, you'll need **UWP Development**, install all recent UWP SDKs, starting from 10.0.19041 (or above or equal to `TargetPlatformVersion` line [in this file](../../../../src/Uno.CrossTargetting.targets))
- Install (**Tools** / **Android** / **Android SDK manager**) all Android SDKs starting from 7.1 (or the Android versions `TargetFrameworks` [list used here](../../../../src/Uno.UI.BindingHelper.Android/Uno.UI.BindingHelper.Android.netcoremobile.csproj))
- Run [Uno.Check](xref:UnoCheck.UsingUnoCheck) on your dev machine to setup .NET Android/iOS workloads
- Install the latest [.NET SDK](https://aka.ms/dotnet/download) from Microsoft.

## Recommended Windows hardware

Loading and building the Uno.UI solution is a resource-intensive task. As a result, opening it in Visual Studio 2022 requires a minimum hardware configuration to avoid spending time waiting for builds.

**Minimum configuration:**

- Intel i7 (8th gen) or equivalent
- 16 GB of RAM
- 250GB of Fast SSD

**Optimal configuration:**

- Intel i9 or equivalent
- 32 GB of RAM
- 500GB M2 SSD

## Building Uno.UI for a single target platform

This is the **recommended** approach to building the Uno.UI solution. It will build a single set of binaries for a particular platform (eg Android, iOS, WebAssembly, etc).

Building for a single target platform is considerably faster, much less RAM-intensive, and generally more reliable.

It involves two things - setting an override for the target framework that will be picked up by the (normally multi-targeted) projects inside the Uno solution, and opening a preconfigured [solution filter](https://learn.microsoft.com/visualstudio/ide/filtered-solutions) which will only load the projects needed for the current platform.

The step-by-step process is:

1. Make sure you don't have the Uno.UI solution opened in any Visual Studio instance. (Visual Studio may crash or behave inconsistently if it's open when the target override is changed)
1. Make a copy of the [`src/crosstargeting_override.props.sample`](../../../../src/crosstargeting_override.props.sample) file and re-name this copy to `src/crosstargeting_override.props`.
1. In `crosstargeting_override.props`, uncomment the line `<UnoTargetFrameworkOverride>xxx</UnoTargetFrameworkOverride>`
1. Set the build target inside `<UnoTargetFrameworkOverride></UnoTargetFrameworkOverride>` to the identifier for the target platform you wish to build for (Identifiers for each platform are listed in the `crosstargeting_override.props` file), then save the file.
1. In the `src` folder, look for the solution filter (`.slnf` file) corresponding to the target platform override you've set, which will be named `Uno.UI-[Platform]-only.slnf` (or the name listed in `crosstargeting_override.props` for the selected `UnoTargetFrameworkOverride`), and open it.
1. To confirm that everything works:
   - For iOS/Android native you can right-click on the `Uno.UI` project
   - For WebAssembly/native, you can right-click on the `Uno.UI.Runtime.WebAssembly` project
   - For Skia Desktop, you can right-click on the corresponding `Uno.UI.Runtime.Skia.[Win32|X11|macOS|iOS|Android|Wpf]` project
1. Optionally adjust additional parameters in `crosstargeting_override.props`, such as `UnoDisableNetAnalyzers`, which can improve the build time during debugging sessions.

Once you've built successfully, for the next steps, [consult the guide here](xref:Uno.Contributing.DebuggingUno) for debugging Uno.UI.

> [!IMPORTANT]
> You will need to repeat the above steps 2. and subsequent when changing the active `UnoTargetFrameworkOverride` value.

If you've followed the steps above, you have your environment set up with the listed prerequisites, and you still encounter errors when you try to build the solution, you can reach out to the core team on Uno's [Discord Server](https://platform.uno/discord).

### Windows and long paths issues

If the build tells you that `LongPath` is not enabled, you may enable it on Windows 10 by using :

```bash
reg ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem /v LongPathsEnabled /t REG_DWORD /d 1
```

If for some reason you cannot modify the registry, you can disable this warning by adding `<UnoUIDisableLongPathWarning>false</UnoUIDisableLongPathWarning>` to the project.

Note that long paths may be required when building Uno, and invalid paths errors may arise.

## Building Uno.UI for all available targets

It's recommended to build using the single-target approach, but it's also possible to build for all targets at once, if you wish.

1. If you've previously followed the single-target steps, comment out the `<UnoTargetFrameworkOverride />` line in your `crosstargeting_override.props` file.
2. Open the [Uno.UI.slnx](../../../../src/Uno.UI.slnx)
3. Select the `Uno.UI` project
4. Build

Inside Visual Studio, the number of platforms is restricted to limit the compilation time.

## Troubleshooting build issues

Here are some tips when building the Uno solution and failures happen:

- Make sure to be on the latest master commit
- Try to close VS 2022, delete the `src/.vs` folder, then try rebuilding the solution
- If the `.vs` deletion did not help, run `git clean -fdx` (after having closed Visual Studio) before building again
- Make sure to have a valid `UnoTargetFrameworkOverride` which matches your solution filter
- Make sure to have the Windows SDK `19041` installed

## Other build-related topics

### Building the reference assemblies for Skia and WebAssembly

Skia and WebAssembly+Native use a custom bait-and-switch technique for assemblies for which the `netX.0` target framework assemblies (called reference assemblies) found in NuGet packages (`lib` folder) are only used for building applications. At the end of a head build, those reference assemblies are replaced by public API compatible assemblies located in the `uno-runtime\[target-framework]` folder of NuGet packages.

When developing a feature using solution filters, if new public APIs are added, building the Uno.UI solution will not update the reference assemblies, causing applications or libraries using the overridden NuGet cache to be unable to use those newly added APIs.

In order to update those reference assemblies, set `<UnoTargetFrameworkOverride>...</UnoTargetFrameworkOverride>` to `netX.0`, then open the `Uno.UI-Reference-Only.slnf` filter. You can now build the `Uno.UI` project. Doing this will generate the proper assemblies with the new APIs to be used in applications or libraries using the NuGet cache override.

### Using the Package Diff tool

Refer to the [guidelines for breaking changes](xref:Uno.Contributing.BreakingChanges) document.

### Updating the Nuget packages used by the Uno.UI solution

The versions used are centralized in the [Directory.Build.targets](../../../../src/Directory.Build.targets) file, and all the
locations where `<PackageReference />` are used.

When updating the versions of NuGet packages, make sure to update all the .nuspec files in the [`build/nuget` folder](../../../../build/nuget).

### Running the SyncGenerator tool

Uno Platform uses a tool which synchronizes all WinRT and WinUI APIs with the type implementations already present in Uno. This ensures that all APIs are present for consumers of Uno, even if some are not implemented.

The synchronization process takes the APIs provided by the WinMD files referenced by the `Uno.UWPSyncGenerator.Reference` project and generates stubbed classes for types in the `Generated` folders of Uno.UI, Uno.Foundation and Uno.WinRT projects. If the generated classes have been partially implemented in Uno (in the non-Generated folders), the tool will automatically skip those implemented methods.

The tool needs to be run on Windows because of its dependency on the Windows SDK WinMD files.

To run the synchronization tool:

- Open a `Developer Command Prompt for Visual Studio` (2019 or 2022)
- Run [`uno\build\run-api-sync-tool.cmd`](../../../../build/run-api-sync-tool.cmd) (! not the `uno\src\build` folder !) you can do so by following the link on this text.
- make sure to follow the instructions <!-- TODO: Add those "Instructions" here -->

Note that the tool needs to be run manually and is not run as part of the CI.

### Android Resources ID generation

To workaround a performance issue, all `Resource.designer.cs` generation is disabled for class libraries in this repo.

If you need to add a new `@(AndroidResource)` value to be used from C# code inside of Uno.UI libraries:

1. Comment out the `<PropertyGroup>` in `Directory.Build.targets` that sets `$(AndroidGenerateResourceDesigner)` and `$(AndroidUseIntermediateDesignerFile)` to `false`.

2. Build Uno.UI as you normally would. You will get compiler errors about duplicate fields, but `obj\Debug\net6.0-android\Resource.designer.cs` should now be generated.

3. Open `obj\Debug\net6.0-android\Resource.designer.cs`, and find the
   field you need such as:

    ```csharp
    // aapt resource value: 0x7F010000
    public static int foo = 2130771968;
    ```

4. Copy this field to the `Resource.designer.cs` checked into source
   control, such as: `src/Uno.UI/Resources/Resource.designer.g.Android.cs`

5. Restore the commented code in `Directory.Build.targets`.

_This performance optimization is inspired by @jonathanpeppers's performance work done in the [dotnet Maui Pull Request 2606](https://github.com/dotnet/maui/pull/2606). Thanks Jonathan!_
