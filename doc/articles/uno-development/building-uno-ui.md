# Building Uno.UI

Prerequisites:
- Visual Studio 2019 (16.3 or later)
    - `Mobile Development with .NET` (Xamarin) development
    - `Visual Studio extensions development` (for the VSIX projects)
    - `ASP.NET and Web Development`
    - `.NET Core cross-platform development`
    - `UWP Development`, install all recent UWP SDKs, starting from 10.0.14393 (or above or equal to `TargetPlatformVersion` line [in this file](https://github.com/unoplatform/uno/blob/master/src/Uno.CrossTargetting.props))
- Install (**Tools** / **Android** / **Android SDK manager**) all Android SDKs starting from 7.1 (or the Android versions `TargetFrameworks` [list used here](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.BindingHelper.Android/Uno.UI.BindingHelper.Android.csproj))

## Building Uno.UI for all available targets
* Open the [Uno.UI.sln](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.sln)
* Select the `Uno.UI` project
* Build

Inside Visual Studio, the number of platforms is restricted to limit the compilation time.

## Faster dev loop with single target-framework builds
To enable faster development, it's possible to use the [Visual Studio Solution Filters](https://docs.microsoft.com/en-us/visualstudio/ide/filtered-solutions?view=vs-2019), and only load the projects relevant for the task at hand.

For instance, if you want to debug an iOS feature:
- Make sure the `Uno.UI.sln` solution is not opened in Visual Studio.
- Make a copy of the [src/crosstargeting_override.props.sample](https://github.com/unoplatform/uno/blob/master/src/crosstargeting_override.props.sample) file to `src/crosstargeting_override.props`
- In this new file, uncomment the `UnoTargetFrameworkOverride` line and set its value to `xamarinios10`
- Open the `Uno.UI-iOS-only.slnf` solution filter (either via the VS folder view, or the Windows explorer)
- Build

This technique works for `xamarinios10`, `monoandroid90`, `netstandard2.0` (wasm), and `net461` (Unit Tests).

> Note that it's very important to close Visual Studio when editing the `src/crosstargeting_override.props` file, otherwise VS may crash or behave inconsistently.

## Using the Package Diff tool

Refer to the [guidelines for breaking changes](../contributing/guidelines/breaking-changes.md) document.

## Building Uno.UI for macOS using Visual Studio for Mac

See [instructions here](building-uno-macos.md) for building Uno.UI for the macOS platform.

## Updating the Nuget packages used by the Uno.UI solution
The versions used are centralized in the [Directory.Build.targets](https://github.com/unoplatform/uno/blob/master/src/Directory.Build.targets) file, and all the
locations where `<PackageReference />` are used.

When updating the versions of nuget packages, make sure to update all the .nuspec files in the [Build folder](https://github.com/unoplatform/uno/tree/master/build).
