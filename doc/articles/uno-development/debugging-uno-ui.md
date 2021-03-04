# Debugging Uno.UI

> [!Note]
> [Find instructions for building the Uno.UI solution here.](building-uno-ui.md) 

## Debugging Uno.UI samples

To debug the SamplesApp in the Uno.UI solution, which includes an extensive set of samples and test cases for the controls supported by Uno.UI, as well as non-UI features:

1. Clone the repo, and ensure using a short target path, e.g. _D:\uno_ etc.  
This is due to limitations in the legacy .NET versions used by Xamarin projects. This issue has been addressed in .NET 5, and will come to the rest of the projects in the future.
2. Open the solution filter in Visual Studio for the target platform you wish to run on, [as detailed here](building-uno-ui.md).
3. Set `SamplesApp.[TargetPlatform]` as the selected Startup Project.
4. Launch the samples app from Visual Studio.

See [this article](working-with-the-samples-apps.md) for more information on working with the SamplesApp and authoring new samples.

## Debugging Uno in another application

It's also easy to debug Uno.UI code in an application outside the Uno.UI solution. The Uno.UI build process has an opt-in mechanism to overwrite the contents of the NuGet cache, causing the application to use your local build of Uno.

This is useful if you're debugging a problem that can't easily be reproduced outside the context of the app where it was discovered. 

It can even speed up your development loop when working on a new feature or fixing a bug with a standalone repro, because a small 'Hello World' app builds considerably faster than the full SamplesApp.

First, you'll need to install, in the app you want to debug with, a published Uno package version which is close to your branch's code, preferably a `-dev.xxx` version. Make sure update all `Uno.UI.*` packages to the same version.

Then, here are the steps to use a local build of Uno.UI in another application:

1. Configure Uno.UI to build for the target platform you wish to debug, [as detailed here](building-uno-ui.md).
2. Close any instances of Visual Studio with Uno.UI open.
3. Open the solution containing the application you wish to debug.
4. Note the NuGet version of Uno.UI (or Uno.UI.WebAssembly/Uno.UI.Skia) being used by the application (eg `3.0.17`).
5. In `src/crosstargeting_override.props`, uncomment the line `<!--<UnoNugetOverrideVersion>xx.xx.xx-dev.xxx</UnoNugetOverrideVersion>-->`.
6. Replace the version number with the version being used by the application you wish to debug.
7. Open the appropriate Uno.UI solution filter and build the **Uno.UI** project (or **Uno.UI.WebAssembly**/**Uno.UI.Skia** projects for WebAssembly or Skia). Be aware that this will **overwrite your local NuGet cache** for the nominated Uno.UI version. Any applications that you build locally will use your local build if they depend on that Uno.UI version.

To debug Uno.UI code in the application, follow these steps (using `FrameworkElement.MeasureOverride()` as an example):

1. Open [`FrameworkElement.cs`](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/FrameworkElement.cs) in the Uno.UI solution.
2. Right-click on the `FrameworkElement.cs` tab header in Visual Studio and choose 'Copy Full Path'. 
3. Switch to the Visual Studio instance where your application is open.
4. In your application solution, choose File->Open->File... or simply `Ctrl+O`, paste the path to `FrameworkElement.cs` into the file open dialog, and open `FrameworkElement.cs` in the application solution.
5. Put a breakpoint in the `MeasureOverride()` method.
6. Launch the application.
7. You should hit the breakpoint, opening the `FrameworkElement.cs` file, and be able to see local variable values, etc.
8. To revert to the original Uno.UI version from NuGet, simply navigate to the NuGet cache folder (`%USERPROFILE%\.nuget\packages`) and delete the `Uno.UI` folder within it. You may need to close Visual Studio first. The original version will be automatically restored the next time the application builds.

### Troubleshooting
It may happen that the package cache for the version you're debugging is corrupted, and the override is not working as intended.

If this is the case:
- In your debugged app, select another package version you've never debugged with
- Make sure to build the app once to populate the nuget cache
- Rebuild the Uno.UI project (or **Uno.UI.WebAssembly**/**Uno.UI.Skia**) to replace the binaries with your debug ones
- Rebuild your app and debug your again

## Microsoft Source Link support
Uno.UI supports [SourceLink](https://github.com/dotnet/sourcelink/) and it now possible to
step into Uno.UI without downloading the repository.

Make sure **Enable source link support** check box is checked in **Tools** / **Options**
/ **Debugging** / **General** properties page.
