---
uid: Uno.Contributing.DebuggingUno
---

# Debugging Uno.UI

> [!NOTE]
> [Find instructions for building the Uno.UI solution here.](xref:Uno.Contributing.BuildingUno)

## Debugging Uno.UI samples

To debug the **SamplesApp** in the Uno.UI solution, which includes an extensive set of samples and test cases for the controls supported by Uno.UI, as well as non-UI features:

1. Clone the repo, and ensure using a short target path, e.g. _D:\uno_ etc.  
   > [!NOTE]
   > This is required to avoid path length limitations in the .NET framework being used by Visual Studio.
2. Open the solution filter in Visual Studio for the target platform you wish to run on, [as detailed here](xref:Uno.Contributing.BuildingUno).
3. Set `SamplesApp.[TargetPlatform]` as the selected Startup Project.
4. Launch the samples app from Visual Studio.

See [this article](working-with-the-samples-apps.md) for more information on working with the SamplesApp and authoring new samples.

## Debugging Uno in another application

It is also possible to debug Uno.UI code in an application outside the Uno.UI solution. The Uno.UI build process has an opt-in mechanism to overwrite the contents of the NuGet cache, causing the application to use your local build of Uno.

> [!NOTE]
> This will **overwrite your local NuGet cache**.

This is useful when debugging a problem that can't easily be reproduced outside the context of the app where it was discovered.

It can even speed up your development loop when working on a new feature or fixing a bug with a standalone repro, because a small 'Hello World' app builds considerably faster than the full SamplesApp.

First, update `global.json` of the app you will be debugging to instead use a `-dev.xxx` version:

```diff
 {
   // To update the version of Uno please update the version of the Uno.Sdk here. See https://aka.platform.uno/upgrade-uno-packages for more information.
   "msbuild-sdks": {
-    "Uno.Sdk": "6.1.23"
+    "Uno.Sdk": "6.2.0-dev.59"
   },
```

The versions of Uno.Sdk are listed on [NuGet.org](https://www.nuget.org/packages/Uno.Sdk/). Typically, you want to use the latest one, in order to match the most recent commit on the `master` branch of uno.

Once `global.json` has been updated, restore your app's solution:

```dotnetcli
dotnet restore
```

Next, determine the _`$(UnoNugetOverrideVersion)` value_ for your selected `-dev.xxx` version:

1. View the specified `Uno.Sdk` package version on NuGet.org, e.g. <https://www.nuget.org/packages/Uno.Sdk/6.2.0-dev.59>
1. Select the **README** tab.
1. On the **README** tab, determine the value of the `UnoVersion*` property.

    For the Uno.Sdk 6.2.0-dev.59 NuGet package, the `UnoVersion*` property is `6.2.0-dev.171`.

    This is the `$(UnoNugetOverrideVersion)` value.

Then, here are the steps to use a local build of Uno.UI in another application:

1. Configure the Uno.UI solution to build for the target platform you wish to debug, [as detailed here](xref:Uno.Contributing.BuildingUno).
1. Close any instances of Visual Studio with the Uno.UI solution opened.
1. Open the solution containing the application you wish to debug to ensure the package is restored and cached.
1. Prepare the application
1. Make a copy of `src/crosstargeting_override.props.sample` and name it as `src/crosstargeting_override.props`.
1. In `src/crosstargeting_override.props`, uncomment the line `<!--<UnoNugetOverrideVersion>xx.xx.xx-dev.xxx</UnoNugetOverrideVersion>-->` and set the `$(UnoNugetOverrideVersion)` value to the value determined in the previous section, e.g. `6.2.0-dev.171`.  You want to use the `UnoVersion*` version here. _Do not mix it up with `Uno.Sdk` version_.
1. Open the appropriate Uno.UI solution filter and build the following:
   - For iOS/Android native, you can right-click on the `Uno.UI` project
   - For WebAssembly/native, you can right-click on the `Uno.UI.Runtime.WebAssembly` project
   - For Skia, you can right-click on the corresponding `Uno.UI.Runtime.Skia.[Win32|X11|macOS|iOS|Android|Wpf]` project.

To debug Uno.UI code in the application, follow these steps (using `FrameworkElement.MeasureOverride()` as an example):

1. Open [`FrameworkElement.cs`](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/FrameworkElement.cs) in the Uno.UI solution.
2. Right-click on the `FrameworkElement.cs` tab header in Visual Studio and choose 'Copy Full Path'.
3. Switch to the Visual Studio instance where your application is open.
4. In your application solution, choose File->Open->File... or simply `Ctrl+O`, paste the path to `FrameworkElement.cs` into the file open dialog, and open `FrameworkElement.cs` in the application solution.
5. Put a breakpoint in the `MeasureOverride()` method.
6. Launch the application.
7. You should hit the breakpoint, opening the `FrameworkElement.cs` file, and be able to see local variable values, etc.
8. To revert to the original Uno.UI version from NuGet, simply navigate to the NuGet cache folder (`%USERPROFILE%\.nuget\packages`) and delete the `Uno.UI` folder within it. You may need to close Visual Studio first. The original version will be automatically restored the next time the application builds.

### Tips about the NuGet version override process

- Be aware that setting `UnoNugetOverrideVersion` will **overwrite your local NuGet cache** for the nominated Uno.UI version. Any applications that you build locally will use your local build if they depend on that Uno.UI version.

- Building for Android requires the API level to match the version you specified in `UnoTargetFrameworkOverride`. A common issue is that the app being debugged uses Android 10.0, and `Uno.UI` is built using Android 11.0. You can change the API level in the debugged project's build properties.

- The NuGet override process only works on already installed versions. The best way to ensure that the override is successful is to build the debugged application once before overriding the Uno.UI version the app uses.

- Make sure to close the application that uses the overridden nuget package, to avoid locked files issues on Windows.

- Verify that your updated files are actually used.  View [diagnostic logs](https://learn.microsoft.com/en-us/visualstudio/msbuild/obtaining-build-logs-with-msbuild?view=vs-2022), search for the assembly that you are modifying, and verify that it contains the updated `$(UnoNugetOverrideVersion)` value in the path, e.g. that the `@(RuntimeCopyLocalItems)` item group contains `%USERPROFILE%\.nuget\packages\uno.winrt\6.2.0-dev.171\lib\net9.0-android30.0\Uno.UI.Dispatching.dll` (Windows) or `$HOME/.nuget/packages/uno.winrt/6.2.0-dev.171/lib/net9.0-android30.0/Uno.UI.Dispatching.dll` (Linux, macOS).

### Troubleshooting

It may happen that the package cache for the version you're debugging is corrupted, and the override is not working as intended.

If this is the case:

- In your debugged app, install another package version you've never debugged with
- Make sure to build the app once to populate the NuGet cache
- Rebuild the Uno.UI project (or **Uno.UI.WebAssembly**/**Uno.UI.Runtime.Skia.\***) to replace the binaries with your debug versions
- Rebuild your app and debug it again

## Microsoft Source Link support

Uno.UI supports [SourceLink](https://github.com/dotnet/sourcelink/) and it now possible to
step into Uno.UI without downloading the repository.

Make sure **Enable source link support** check box is checked in **Tools** / **Options**
/ **Debugging** / **General** properties page.
