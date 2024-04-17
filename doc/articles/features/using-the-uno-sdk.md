---
uid: Uno.Features.Uno.Sdk
---

# Using the Uno.Sdk

Uno Platform projects use the Uno.Sdk, and msbuild package designed to keep projects simple, yet configurable. It inherits from the `Microsoft.Net.Sdk` and `Microsoft.Net.Sdk.Web` depending on the platform.

This document explains the many features of this SDK, and how to configure its behavior.

> [!NOTE]
> The Uno.Sdk only supports the WinUI API set.
> [!TIP]
> The Uno.Sdk enabled projects are best experienced using the [MSBuild Editor Visual Studio 2022 Extension](https://marketplace.visualstudio.com/items?itemName=mhutch.msbuildeditor) to provide intellisense.

## Managing the Uno.Sdk version

Updating the Uno.Sdk is [done through the global.json file](xref:Uno.Development.UpgradeUnoNuget).

## Uno Platform Features

As Uno Platform can be used in many different ways, in order to reduce the build time and avoid downloading many packages, the Uno.Sdk offers a way to specify which Uno Platform features should be enabled.

In the csproj of an app, you will find the following property:

```xml
<UnoFeatures>
    Material;
    Hosting;
    Toolkit;
    Logging;
    Serilog;
    Mvux;
    Configuration;
    Http;
    Serialization;
    Localization;
    Navigation;
</UnoFeatures>
```

> [!IMPORTANT]
> Once you have changed the features list, Visual Studio requires restoring packages explicitly, or building the app once for the change to take effect.

This allows for the SDK to selectively include references to relevant sets of Nuget packages to enable features for your app.

Here are the supported features:

| feature | description |
|---------|-------------|
| `Foldable` | Adds a reference to Uno.WinUI.Foldable |
| `MediaElement` | Adds a native references where needed to use MediaElement |
| `CSharpMarkup` | Adds support for C# Markup|
| `Extensions` | Adds the most commonly used Extensions Packages for Hosting, Configuration, and Logging |
| `Authentication` | Adds the Uno.Extensions package for Custom Authentication |
| `AuthenticationMsal` | dds the Uno.Extensions packages for Authentication using Microsoft.Identity.Client |
| `AuthenticationOidc` | Adds the Uno.Extensions packages for Authentication using a custom Oidc client |
| `Configuration` | Adds the Uno.Extensions packages for Configuration |
| `Hosting` | Adds support for Dependency Injection using Uno.Extensions.Hosting packages |
| `Http` | Adds support for custom Http Clients including the use of Refit with Uno.Extensions |
| `Localization` | Adds support for Localization using Uno.Extensions |
| `Logging` | Adds support for Logging using Uno.Extensions |
| `MauiEmbedding` | Adds support for embedding Maui controls in Uno |
| `MVUX` | Adds support for MVUX |
| `ThemeService` | Adds the Uno.Extensions.Core.WinUI package |
| `Navigation` | Adds support for Navigation using Uno.Extensions |
| `LoggingSerilog` | Adds support for Serilog using Uno.Extensions |
| `Storage` | Adds support for Storage using Uno.Extensions |
| `Serialization` | Adds support for Uno.Extensions.Serialization |
| `Toolkit` | Adds support for the Uno.Toolkit |
| `Material` | Adds support for the Material Design Toolkit |
| `Cupertino` | Adds support for the Cupertino Design Toolkit |
| `Mvvm` | Adds support for the CommunityToolkit.Mvvm package |
| `Dsp` | Adds support for the Uno.Dsp package |
| `Prism` | Adds Prism support for Uno.WinUI |
| `Skia` | Adds support for SkiaSharp |
| `Svg` | Svg support for iOS, Android, and mac Catalyst. This option is not needed for WebAssembly, Desktop, and WinAppSDK. |
| `Lottie` | Adds support for Lottie animations |

> [!IMPORTANT]
> Feature names are case sensitive

## Implicit Packages

Uno Platform is composed of many required and optional NuGet packages. By default, the SDK automatically references the Uno.UI required packages based on the current target framework, using versions appropriate for the version of Uno Platform being used.

It is possible to configure the version of those packages in two ways. The first is by using an explicit `PackageReference` to any of the Uno Platform packages, or by using the `*Version` properties supported by the SDK. These versions are used by the `UnoFeatures` defined for your app.

Here are the supported properties:

## [**Uno Platform Packages**](#tab/uno-packages)

- `UnoCoreLoggingSingletonVersion`
- `UnoCSharpMarkupVersion`
- `UnoDspTasksVersion`
- `UnoExtensionsVersion`
- `UnoLoggingVersion`
- `UnoResizetizerVersion`
- `UnoThemesVersion`
- `UnoToolkitVersion`
- `UnoUniversalImageLoaderVersion`
- `UnoWasmBootstrapVersion`

## [**Third Party Packages**](#tab/3rd-party-packages)

- `AndroidMaterialVersion`
- `AndroidXActivityVersion`
- `AndroidXAppCompatVersion`
- `AndroidXBrowserVersion`
- `AndroidXCollectionVersion`
- `AndroidXLegacySupportV4Version`
- `AndroidXNavigationVersion`
- `AndroidXRecyclerViewVersion`
- `AndroidXSwipeRefreshLayoutVersion`
- `CommunityToolkitMvvmVersion`
- `MicrosoftIdentityClientVersion`
- `MicrosoftLoggingVersion`
- `PrismVersion`
- `SkiaSharpVersion`
- `WinAppSdkBuildToolsVersion`
- `WinAppSdkVersion`
- `WindowsCompatibilityVersion`

***

Those properties can be set from `Directory.Build.props`.

If you wish to disable Implicit package usage, add `<DisableImplicitUnoPackages>true</DisableImplicitUnoPackages>` to your `Directory.Build.props` file. You will be then able to manually add the NuGet packages for your project.

## Supported OS Platform versions

By default, the Uno.Sdk specifies a set of OS Platform versions, as follows:

| Target | `SupportedOSPlatformVersion` |
|--------|----------------------------|
| Android | 21 |
| iOS | 14.2 |
| macOS | 10.14 |
| MacCatalyst | 14.0 |
| WinUI | 10.0.18362.0 |

You can set this property in a `Choose` MSBuild block in order to alter its value based on the active `TargetFramework`.

## Visual Studio 2022 First-TargetFramework workarounds

Using a Single Project in Visual Studio 2022 requires the Uno Platform tooling to apply workarounds in order to have an acceptable debugging experience.

For some of the platforms (Desktop, WinAppSDK and WebAssembly), the corresponding target frameworks must be placed first in order for debugging and publishing to function properly. To adress that problem, the Uno Platform tooling modifies the `csproj` file to reorder the `TargetFrameworks` property so that the list is accepted by Visual Studio.

As a result, the csproj file on disk and will show the file as modified in you source control, yet the automatic change can be reverted safely. If the behavior is impacting your IDE negatively, you can disable it by adding the following in your `.csproj` file:

```xml
<PropertyGroup>
  <UnoDisableVSTargetFrameworksRewrite>true</UnoDisableVSTargetFrameworksRewrite>
</PropertyGroup>
```

Note that we are currently tracking these Visual Studio issues, make sure to upvote them:

- `net8.0-desktop` must be first for WSL debugging to work (**Link to be available soon**)
- `net8.0-browserwasm` must be first for WebAssembly debugging to work (**Link to be available soon**)
- `net8.0-desktop` being first breaks all other targets debugging (**Link to be available soon**)
- `net8.0-windows10` needs to be first for WinAppSDK Hot reload to work (**Link to be available soon**)

## Disabling Default Items

The `Uno.Sdk` will automatically includes files that you previously needed to manage within your projects. These default items include definitions for including files within the `Content`, `Page`, and `PRIResource` item groups. Additionally, if you have referenced the `Uno.Resizetizer` it will add default items for the `UnoImage` allowing you to more easily manage your image assets.

You may disable this behavior in one of two ways:

```xml
<PropertyGroup>
  <!-- Globally disable all default includes from the `Uno.Sdk`, `Microsoft.NET.Sdk`, and if building on WASM `Microsoft.NET.Sdk.Web` -->
  <EnableDefaultItems>false</EnableDefaultItems>

  <!-- Disable only default items provided by the `Uno.Sdk` -->
  <EnableDefaultUnoItems>false</EnableDefaultUnoItems>
</PropertyGroup>
```

## WinAppSdk PRIResource Workaround

Many Uno projects and libraries make use of a `winappsdk-workaround.targets` file that corrects a [bug](https://github.com/microsoft/microsoft-ui-xaml/issues/8857) found in WinUI. When using the `Uno.Sdk` these targets now are provided for you out of the box. This extra set of workaround targets can be disabled by setting the following property:

```xml
<PropertyGroup>
  <DisableWinUI8857_Workaround>true</DisableWinUI8857_Workaround>
</PropertyGroup>
```

## Cross Targeting Support

By Default when using the Uno.Sdk you get the added benefit of default includes for an easier time building Cross Targeted Applications. The supported file extensions are as shown below:

- `*.crossruntime.cs` (WASM, Skia, or Reference)
- `*.wasm.cs`
- `*.skia.cs`
- `*.reference.cs`
- `*.iOS.cs`(iOS & MacCatalyst)
- `*.macOS.cs` (MacOS not MacCatalyst)
- `*.iOSmacOS.cs` (iOS, MacCatalyst, & MacOS)
- `*.Android.cs`

As discussed above setting `EnableDefaultUnoItems` to false will disable these includes.
