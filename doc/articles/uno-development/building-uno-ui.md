# Building Uno.UI

This article explains how to build Uno.UI locally, for instance if you wish to contribute a bugfix or new feature.

## Prerequisites

- Visual Studio 2019 (16.6 or later)
    - `Mobile Development with .NET` (Xamarin) development
    - `Visual Studio extensions development` (for the VSIX projects)
    - `ASP.NET and Web Development`
    - `.NET Core cross-platform development`
    - `UWP Development`, install all recent UWP SDKs, starting from 10.0.14393 (or above or equal to `TargetPlatformVersion` line [in this file](https://github.com/unoplatform/uno/blob/master/src/Uno.CrossTargetting.props))
- Install (**Tools** / **Android** / **Android SDK manager**) all Android SDKs starting from 7.1 (or the Android versions `TargetFrameworks` [list used here](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.BindingHelper.Android/Uno.UI.BindingHelper.Android.csproj))

## Building Uno.UI for a single target platform

This is the **recommended** approach to building the Uno.UI solution. It will build a single set of binaries for a particular platform (eg Android, iOS, WebAssembly, etc). 

Building for a single target platform is considerably faster, much less RAM-intensive, and generally more reliable.

It involves two things - setting an override for the target framework that will be picked up by the (normally multi-targeted) projects inside the Uno solution; and opening a preconfigured [solution filter](https://docs.microsoft.com/en-us/visualstudio/ide/filtered-solutions) which will only load the projects needed for the current platform.

The step by step process is:

1. Clone the Uno.UI repository locally.
2. Make sure you don't have the Uno.UI solution open in any Visual Studio instances. (Visual Studio may crash or behave inconsistently if it's open when the target override is changed.)
3. Make a copy of the [src/crosstargeting_override.props.sample](https://github.com/unoplatform/uno/blob/master/src/crosstargeting_override.props.sample) file and name this copy `src/crosstargeting_override.props`.
4. In `crosstargeting_override.props`, uncomment the line `<UnoTargetFrameworkOverride>netstandard2.0</UnoTargetFrameworkOverride>`
5. Set the build target inside ``<UnoTargetFrameworkOverride></UnoTargetFrameworkOverride>`` to the identifier for the target platform you wish to build for. (Identifiers for each platform are listed in the file.) Save the file.
6. In the `src` folder, look for the solution filter (`.slnf` file) corresponding to the target platform override you've set, which will be named `Uno.UI-[Platform]-only.slnf`, and open it.
7. To confirm that everything works, you can right-click on the `Uno.UI` project in the Solution Explorer and 'Build'. (The project will be `Uno.UI.Wasm` or `Uno.UI.Skia` if you're targeting Wasm or Skia.)

Once you've built successfully, for the next steps, [consult the guide here](debugging-uno-ui.md) for debugging Uno.UI.

If you've followed the steps above, you have your environment set up with the listed prerequisites, and you still encounter errors when you try to build the solution, you can reach out to the core team on Uno's [Discord channel #uno-platform](https://discord.gg/eBHZSKG).


## Building Uno.UI for all available targets

It's recommended to build using the single-target approach, but it's also possible to build for all targets at once, if you wish.

1. If you've previously followed the single-target steps, comment out the `<UnoTargetFrameworkOverride />` line in your `crosstargeting_override.props` file.
2. Open the [Uno.UI.sln](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.sln)
3. Select the `Uno.UI` project
4. Build

Inside Visual Studio, the number of platforms is restricted to limit the compilation time.

## Building Uno.UI for macOS using Visual Studio for Mac

See [instructions here](building-uno-macos.md) for building Uno.UI for the macOS platform.

## Other build-related topics 

### Using the Package Diff tool

Refer to the [guidelines for breaking changes](../contributing/guidelines/breaking-changes.md) document.

### Updating the Nuget packages used by the Uno.UI solution
The versions used are centralized in the [Directory.Build.targets](https://github.com/unoplatform/uno/blob/master/src/Directory.Build.targets) file, and all the
locations where `<PackageReference />` are used.

When updating the versions of nuget packages, make sure to update all the .nuspec files in the [Build folder](https://github.com/unoplatform/uno/tree/master/build).
