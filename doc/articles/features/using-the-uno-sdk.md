---
uid: Uno.Features.Uno.Sdk
---

# Using the Uno.Sdk

Uno Platform projects use the Uno.Sdk, and msbuild package designed to keep projects simple, yet configurable. It inherits from the `Microsoft.Net.Sdk` and `Microsoft.Net.Sdk.Web` depending on the platform.

This document explains the many features of this SDK, and how to change its behavior.

> [!NOTE]
> The Uno.Sdk only supports the WinUI API set.

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

This allows for the SDK to selectively include references to relevant sets of Nuget packages to enable features for your app.

Here are the supported features:

- `Maps`, to enable Maps support
- `Foldable`, to enable Android foldable support
- `MediaElement`, to enable MediaElement support
- `CSharpMarkup`, to enable C# Markup
- `Extensions`, to enable all Uno.Extensions
- `Authentication`, to enable all Uno.Extensions.Authentication
- `AuthenticationMsal`, to enable Uno.Extensions support for MSAL
- `AuthenticationOidc`, to enable Uno.Extensions support for OIDC
- `Configuration`, to enable Uno.Extensions.Configuration
- `Hosting`, to enable Uno.Extensions.Hosting
- `Http`, to enable Uno.Extensions.Http
- `Localization`, to enable Uno.Extensions.Localization
- `Logging`, to enable Uno.Extensions.Logging
- `MauiEmbedding`, to enable Uno.Extensions.MauiEmbedding
- `Mvux`, to enable Uno.Extensions.Reactive
- `Navigation`, to enable Uno.Extensions.Navigation
- `Serilog`, to enable Uno.Extensions.Logging.Serilog
- `Storage`, to enable Uno.Extensions.Storage
- `Serialization`, to enable Uno.Extensions.Serialization
- `Toolkit`, to enable Uno.Toolkit.UI
- `Material`, to enable Uno.Material
- `Cupertino`, to enable Uno.Cupertino
- `Mvvm`, to enable CommunityToolkit.Mvvm
- `Prism`, to enable Prism Library
- `Skia`, to enable SkiaSharp

## Implicit Packages

Uno Platform is composed of many required and optional NuGet packages. By default, the SDK automatically references the Uno.UI required packages based on the current target framework, using versions appropriate for the version of Uno Platform being used.

It is possible to configure the version of those packages in two ways. The first is by using an explicit `PackageReference` to any of the Uno Platform packages, or by using the `*Version` properties supported by the SDK. These versions are used by the `UnoFeatures` defined for your app.

Here are the supported properties:

- `UnoExtensionsVersion`
- `UnoToolkitVersion`
- `UnoThemesVersion`
- `UnoCSharpMarkupVersion`
- `SkiaSharpVersion`
- `UnoLoggingVersion`
- `WindowsCompatibilityVersion`
- `UnoWasmBootstrapVersion`
- `UnoUniversalImageLoaderVersion`
- `AndroidMaterialVersion`
- `AndroidXLegacySupportV4Version`
- `AndroidXAppCompatVersion`
- `AndroidXRecyclerViewVersion`
- `AndroidXActivityVersion`
- `AndroidXBrowserVersion`
- `AndroidXSwipeRefreshLayoutVersion`
- `UnoResizetizerVersion`
- `MicrosoftLoggingVersion`
- `WinAppSdkVersion`
- `WinAppSdkBuildToolsVersion`
- `UnoCoreLoggingSingletonVersion`
- `UnoDspTasksVersion`
- `CommunityToolkitMvvmVersion`
- `PrismVersion`
- `AndroidXNavigationVersion`
- `AndroidXCollectionVersion`
- `MicrosoftIdentityClientVersion`

Those properties can be set from `Directory.Build.props`.

If you wish to disable Implicit package usage, add `<DisableImplicitUnoPackages>true</DisableImplicitUnoPackages>` to your `Directory.Build.props` file. You will be then able to manually add the NuGet packages for your project.

### Supported OS Platform versions

By default, the Uno.Sdk specifies a set of OS Platform versions, as follows:

| Target | `SupportedOSPlatformVersion` |
|--------|----------------------------|
| Android | 21 |
| iOS | 14.2 |
| macOS | 10.14 |
| MacCatalyst | 14.0 |
| WinUI | 10.0.18362.0 |

You can set this property in a `Choose` MSBuild block in order to alter its value based on the active `TargetFramework`.

### Disabling Default Items

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

### WinAppSdk PRIResource Workaround

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
