---
uid: Uno.Features.Uno.Sdk
---

# Using the Uno.Sdk

Uno Platform projects use the Uno.Sdk package that is designed to keep projects simple, yet configurable. It imports the `Microsoft.Net.Sdk` (and the `Microsoft.Net.Sdk.Web` for WebAssembly).

This document explains the many features of this SDK and how to configure its behavior.

> [!TIP]
> Beginning with 5.2, Uno.Sdk enabled projects are best experienced using the [MSBuild Editor Visual Studio 2022 Extension](https://marketplace.visualstudio.com/items?itemName=mhutch.msbuildeditor) to provide intellisense.

## Managing the Uno.Sdk version

Updating the Uno.Sdk is [done through the global.json file](xref:Uno.Development.UpgradeUnoNuget).

## Uno Platform Features

As Uno Platform can be used in many different ways, in order to reduce the build time and avoid downloading many packages, the Uno.Sdk offers a way to simplify which Uno Platform features should be enabled.

You can use the `UnoFeatures` property in the `csproj` or `Directory.Build.props` as shown here:

```xml
<UnoFeatures>
    Material;
    Hosting;
    Toolkit;
    Logging;
    Serilog;
    MVUX;
    Configuration;
    Http;
    HttpRefit;
    HttpKiota;
    Serialization;
    Localization;
    Navigation;
    SkiaRenderer;
</UnoFeatures>
```

> [!IMPORTANT]
> Once you have changed the features list, Visual Studio requires restoring packages explicitly, or building the app once for the change to take effect.

This allows for the SDK to selectively include references to relevant sets of Nuget packages to enable features for your app.

Here are the supported features:

| Feature              | Description                                                                                                                                                                                                                                |
|----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Authentication`     | Adds the [Uno.Extensions](xref:Uno.Extensions.Overview) packages for Custom [Authentication](xref:Uno.Extensions.Authentication.Overview).                                                                                                 |
| `AuthenticationMsal` | Adds the [Uno.Extensions](xref:Uno.Extensions.Overview) packages for [Authentication](xref:Uno.Extensions.Authentication.Overview) using [Microsoft.Identity.Client](xref:Uno.Extensions.Authentication.HowToMsalAuthentication).          |
| `AuthenticationOidc` | Adds the [Uno.Extensions](xref:Uno.Extensions.Overview) packages for [Authentication](xref:Uno.Extensions.Authentication.Overview) using a custom [Oidc](xref:Uno.Extensions.Authentication.HowToOidcAuthentication) client.               |
| `Configuration`      | Adds the [Uno.Extensions](xref:Uno.Extensions.Overview) packages for [Configuration](xref:Uno.Extensions.Configuration.Overview).                                                                                                          |
| `CSharpMarkup`       | Adds support for [C# Markup](xref:Uno.Extensions.Markup.Overview).                                                                                                                                                                         |
| `Cupertino`          | Adds support for the [Cupertino Design Theme](xref:Uno.Themes.Cupertino.GetStarted) library. If the `Toolkit` feature is also used, it will add support for the [Cupertino Design Toolkit](xref:Toolkit.GettingStarted.Cupertino) library. |
| `Dsp`                | Adds support for the [Uno.Dsp.Tasks packages](https://www.nuget.org/packages?q=uno.dsp.tasks).                                                                                                                                             |
| `Extensions`         | Adds the most commonly used Extensions Packages for Hosting, Configuration, and Logging.                                                                                                                                                   |
| `Foldable`           | Adds a reference to [Uno.WinUI.Foldable](https://www.nuget.org/packages/Uno.WinUI.Foldable).                                                                                                                                               |
| `GLCanvas`           | Adds support for the [OpenGL Canvas](xref:Uno.Controls.GLCanvasElement).                                                                                                                                                                   |
| `GooglePlay`         | Adds support for [In App Reviews](xref:Uno.Features.StoreContext). For more information, see the [Store Context documentation](xref:Uno.Features.StoreContext).                                                                            |
| `Hosting`            | Adds support for [Dependency Injection](xref:Uno.Extensions.DependencyInjection.Overview) using [Uno.Extensions.Hosting packages](https://www.nuget.org/packages?q=Uno.Extensions.Hosting).                                                |
| `Http`               | Adds support for custom [Http Clients](xref:Uno.Extensions.Http.Overview) with [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                |
| `HttpRefit`          | Adds support for strongly-typed REST API clients via [Refit](xref:Uno.Extensions.Http.Overview#refit)                                                                           |
| `HttpKiota`          | Adds support for OpenAPI-generated clients via [Kiota](xref:Uno.Extensions.Http.Overview#kiota)                                                                           |
| `Localization`       | Adds support for [Localization](xref:Uno.Extensions.Localization.Overview) using [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                                           |
| `Logging`            | Adds support for [Logging](xref:Uno.Extensions.Logging.Overview) using [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                                                     |
| `LoggingSerilog`     | Adds support for [Serilog](https://github.com/serilog/serilog) using [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                                                       |
| `Lottie`             | Adds support for [Lottie animations](xref:Uno.Features.Lottie).                                                                                                                                                                            |
| `Material`           | Adds support for the [Material Design Theme](xref:Uno.Themes.Material.GetStarted) library. If the `Toolkit` feature is also used, it will add support for the [Material Design Toolkit](xref:Toolkit.GettingStarted.Material) library.     |
| `MauiEmbedding`      | Adds support for [embedding Maui controls in Uno Platform applications](xref:Uno.Extensions.Maui.Overview).                                                                                                                                |
| `MediaPlayerElement`       | Adds native references where needed to use [MediaPlayerElement](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.mediaplayerelement).                                                                                                |
| `Mvvm`               | Adds support for the [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) package.                                                                                                                                |
| `MVUX`               | Adds support for [MVUX](xref:Uno.Extensions.Mvux.Overview).                                                                                                                                                                                |
| `Navigation`         | Adds support for [Navigation](xref:Uno.Extensions.Navigation.Overview) using [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                                               |
| `Prism`              | Adds [Prism](https://github.com/PrismLibrary/Prism) support for Uno Platform applications WinUI.                                                                                                                                           |
| `Serialization`      | Adds support for [Serialization](xref:Uno.Extensions.Serialization.Overview) using [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                                         |
| `Skia`               | Adds support for [SkiaSharp](https://github.com/mono/SkiaSharp).                                                                                                                                                                           |
| `SkiaRenderer`       | Adds support for using Skia as the graphics rendering engine. For more details, see [Skia Rendering documentation](xref:uno.features.renderer.skia).                                                                                               |
| `Storage`            | Adds support for [Storage](xref:Uno.Extensions.Storage.Overview) using [Uno.Extensions](xref:Uno.Extensions.Overview).                                                                                                                     |
| `Svg`                | [SVG](xref:Uno.Features.SVG) support for iOS, and Android. This option is not needed when only targeting WebAssembly and WinAppSDK.                                                                                          |
| `ThemeService`       | Adds the [Uno.Extensions.Core.WinUI package](https://www.nuget.org/packages/Uno.Extensions.Core.WinUI).                                                                                                                                    |
| `Toolkit`            | Adds support for the [Uno.Toolkit](xref:Toolkit.GettingStarted).                                                                                                                                                                           |
| `WebView`            | Adds support for the [WebView2 control](xref:Uno.Controls.WebView2).|

## Implicit Packages

Uno Platform is composed of many required and optional NuGet packages. By default, the SDK automatically references the Uno.UI required packages based on the current target framework, using versions appropriate for the version of Uno Platform being used.

It is possible to configure the version of those packages in two ways. The first is by using an explicit `PackageReference` to any of the Uno Platform packages, or by using the `*Version` properties supported by the SDK. These versions are used by the `UnoFeatures` defined for your app.

Here are the supported properties:

## [**Uno Platform Packages**](#tab/uno-packages)

| Property                         | NuGet Package(s)                                                                                                 | Description                                                                                                                         |
|----------------------------------|------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| `UnoCoreLoggingSingletonVersion` | [Uno.Core.Extensions.Logging.Singleton](https://www.nuget.org/packages/Uno.Core.Extensions.Logging.Singleton)    | Provides a logging singleton pattern with helpers and extension methods for simplified logging.                                     |
| `UnoCSharpMarkupVersion`         | [Uno.WinUI.Markup](https://www.nuget.org/packages/Uno.WinUI.Markup) and similar packages                         | Enables [C# Markup](xref:Uno.Extensions.Markup.Overview), the use of C# for building UI markup, similar to XAML but with C# syntax. |
| `UnoDspTasksVersion`             | [Uno.Dsp.Tasks](https://www.nuget.org/packages/Uno.Dsp.Tasks) and similar packages                               | Includes tasks for Uno DSP (Theme colors import) within Uno Platform projects.                                                      |
| `UnoExtensionsVersion`           | [Uno.Extensions.Storage.WinUI](https://www.nuget.org/packages/Uno.Extensions.Storage.WinUI) and similar packages | Extends the Uno Platform with additional methods and classes for more versatile application development.                            |
| `UnoLoggingVersion`              | [Uno.Extensions.Logging.OSLog](https://www.nuget.org/packages/Uno.Extensions.Logging.OSLog) and similar packages | Implements logging mechanisms to help with monitoring and debugging Uno Platform applications.                                      |
| `UnoResizetizerVersion`          | [Uno.Resizetizer](https://www.nuget.org/packages/Uno.Resizetizer)                                                | Provides tools for automatically resizing and managing image assets in Uno Platform projects.                                       |
| `UnoThemesVersion`               | [Uno.Material.WinUI](https://www.nuget.org/packages/Uno.Material.WinUI) and similar packages                     | Supplies a variety of themes that can be applied to Uno Platform applications to enhance the UI.                                    |
| `UnoToolkitVersion`              | [Uno.Toolkit.WinUI](https://www.nuget.org/packages/Uno.Toolkit.WinUI) and similar packages                       | Offers a collection of controls, helpers, and tools to complement the standard WinUI components.                                    |
| `UnoUniversalImageLoaderVersion` | [Uno.UniversalImageLoader](https://www.nuget.org/packages/Uno.UniversalImageLoader)                              | Facilitates the loading and displaying of images across different platforms supported by Uno.                                       |
| `UnoWasmBootstrapVersion`        | [Uno.Wasm.Bootstrap](https://www.nuget.org/packages/Uno.Wasm.Bootstrap) and similar packages                     | Enables the bootstrapping of Uno Platform applications running on WebAssembly.                                                      |

> [!NOTE]
> In the 5.2 version of the Uno.Sdk you must provide a value for `UnoExtensionsVersion`, `UnoThemesVersion`, `UnoToolkitVersion`, and `UnoCSharpMarkupVersion` in order to use the packages associated with the UnoFeatures from these libraries as they are downstream dependencies of the Uno repository.

## [**Third Party Packages**](#tab/3rd-party-packages)

| Property                            | NuGet Package(s)                                                                                                     | Description                                                                                                    |
|-------------------------------------|----------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
| `AndroidMaterialVersion`            | [Xamarin.Google.Android.Material](https://www.nuget.org/packages/Xamarin.Google.Android.Material)                    | Implements Material Design components for Android using Xamarin bindings.                                      |
| `AndroidXActivityVersion`           | [Xamarin.AndroidX.Activity](https://www.nuget.org/packages/Xamarin.AndroidX.Activity)                                | Provides classes to build Android activities with AndroidX libraries.                                          |
| `AndroidXAppCompatVersion`          | [Xamarin.AndroidX.AppCompat](https://www.nuget.org/packages/Xamarin.AndroidX.AppCompat)                              | Offers backward-compatible versions of Android components with AndroidX.                                       |
| `AndroidXBrowserVersion`            | [Xamarin.AndroidX.Browser](https://www.nuget.org/packages/Xamarin.AndroidX.Browser)                                  | Contains components to implement browser features with AndroidX.                                               |
| `AndroidXCollectionVersion`         | [Xamarin.AndroidX.Collection](https://www.nuget.org/packages/Xamarin.AndroidX.Collection) and similar packages       | Provides AndroidX extensions for collections like sparse arrays and bundles.                                   |
| `AndroidXLegacySupportV4Version`    | [Xamarin.AndroidX.Legacy.Support.V4](https://www.nuget.org/packages/Xamarin.AndroidX.Legacy.Support.V4)              | Supports older Android versions with AndroidX compatibility libraries.                                         |
| `AndroidXSplashScreenVersion`    | [Xamarin.AndroidX.Core.SplashScreen](https://www.nuget.org/packages/Xamarin.AndroidX.Core.SplashScreen)              | Support for Android splash screen customization.                                         |
| `AndroidXNavigationVersion`         | [Xamarin.AndroidX.Navigation.UI](https://www.nuget.org/packages/Xamarin.AndroidX.Navigation.UI) and similar packages | Facilitates navigation within an Android app using AndroidX.                                                   |
| `AndroidXRecyclerViewVersion`       | [Xamarin.AndroidX.RecyclerView](https://www.nuget.org/packages/Xamarin.AndroidX.RecyclerView)                        | Implements a flexible view for providing a limited window into large datasets with AndroidX.                   |
| `AndroidXSwipeRefreshLayoutVersion` | [Xamarin.AndroidX.SwipeRefreshLayout](https://www.nuget.org/packages/Xamarin.AndroidX.SwipeRefreshLayout)            | Provides a swipe-to-refresh UI pattern with AndroidX.                                                          |
| `CommunityToolkitMvvmVersion`       | [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)                                        | Delivers a set of MVVM (Model-View-ViewModel) components for .NET applications.                                |
| `MicrosoftIdentityClientVersion`    | [Microsoft.Identity.Client](https://www.nuget.org/packages/Microsoft.Identity.Client)                                | Provides an authentication library for Microsoft Identity Platform.                                            |
| `MicrosoftLoggingVersion`           | [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)          | Enables logging to the console with Microsoft's extensions.                                                    |
| `PrismVersion`                      | [Prism.Uno.WinUI](https://www.nuget.org/packages/Prism.Uno.WinUI) and similar packages                               | Integrates the Prism library, which aids in building loosely coupled, maintainable, and testable applications. |
| `SkiaSharpVersion`                  | [SkiaSharp.Skottie](https://www.nuget.org/packages/SkiaSharp.Skottie) and similar packages                           | Provides a cross-platform 2D graphics API for .NET platforms based on Google's Skia Graphics Library.          |
| `SvgSkiaVersion`                    | [Svg.Skia](https://www.nuget.org/packages/Svg.Skia)                                                                  | Renders SVG files using the SkiaSharp graphics engine.                                                         |
| `WinAppSdkBuildToolsVersion`        | [Microsoft.Windows.SDK.BuildTools](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools)                  | Contains the tools required to build applications for the Microsoft Windows App SDK.                           |
| `WinAppSdkVersion`                  | [Microsoft.WindowsAppSDK](https://www.nuget.org/packages/Microsoft.WindowsAppSDK)                                    | Provides project templates and tools for building Windows applications.                                        |
| `WindowsCompatibilityVersion`       | [Microsoft.Windows.Compatibility](https://www.nuget.org/packages/Microsoft.Windows.Compatibility)                    | Enables Windows desktop apps to use .NET Core by providing access to additional Windows APIs.                  |

---

Those properties can be set from `Directory.Build.props` or may be set in the `csproj` file for your project.

```xml
<!-- .csproj file -->
<Project Sdk="Uno.Sdk">
  <PropertyGroup>

      ...

      <UnoFeatures>
        Material;
        Dsp;
        Hosting;
        Toolkit;
        Logging;
        MVUX;
        Configuration;
        Http;
        Serialization;
        Localization;
        Navigation;
        ThemeService;
        Mvvm;
        SkiaRenderer;
      </UnoFeatures>

      <UnoToolkitVersion>6.3.6</UnoToolkitVersion>
      <MicrosoftLoggingVersion>9.0.1</MicrosoftLoggingVersion>
      <CommunityToolkitMvvmVersion>8.4.0</CommunityToolkitMvvmVersion>
  </PropertyGroup>
</Project>
```

In the sample above, we are overriding the default versions of the `UnoToolkit`, `MicrosoftLogging`, and `CommunityToolkitMvvm` packages.

## Disabling Implicit Uno Packages

If you wish to disable Implicit package usage, add the following:

```xml
<DisableImplicitUnoPackages>true</DisableImplicitUnoPackages>
```

to your `Directory.Build.props` file or `csproj` file. You will be then able to manually add the NuGet packages for your project.

> [!NOTE]
> When disabling Implicit Uno Packages it is recommended that you use the `$(UnoVersion)` to set the version of the core Uno packages that are versioned with the SDK as the SDK requires `Uno.WinUI` to be the same version as the SDK to ensure proper compatibility.

## Supported OS Platform versions

By default, the Uno.Sdk specifies a set of OS Platform versions, as follows:

| Target | `SupportedOSPlatformVersion` |
|--------|----------------------------|
| Android | 21 |
| iOS | 14.2 |
| macOS | 10.14 |
| tvOS  | 14.2 |
| WinUI | 10.0.18362.0 |

You can set this property in a `Choose` MSBuild block in order to alter its value based on the active `TargetFramework`.

```xml
 <Choose>
    <When Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
      <PropertyGroup>
        <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
      <PropertyGroup>
        <SupportedOSPlatformVersion>14.2</SupportedOSPlatformVersion>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'macos'">
      <PropertyGroup>
        <SupportedOSPlatformVersion>10.14</SupportedOSPlatformVersion>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'tvOS'">
      <PropertyGroup>
        <SupportedOSPlatformVersion>14.2</SupportedOSPlatformVersion>
      </PropertyGroup>
    </When>
    <When Condition="$(TargetFramework.Contains('windows10'))">
      <PropertyGroup>
        <SupportedOSPlatformVersion>10.0.18362.0</SupportedOSPlatformVersion>
        <TargetPlatformMinVersion>10.0.18362.0</TargetPlatformMinVersion>
      </PropertyGroup>
    </When>
  </Choose>
```

## Visual Studio 2022 First-TargetFramework workarounds

Using a Single Project in Visual Studio 2022 requires the Uno Platform tooling to apply workarounds in order to have an acceptable debugging experience.

For some of the platforms (Desktop, WinAppSDK, and WebAssembly), the corresponding target frameworks must be placed first in order for debugging and publishing to function properly. To address that problem, the Uno Platform tooling modifies the `csproj` file to reorder the `TargetFrameworks` property so that the list is accepted by Visual Studio.

As a result, the csproj file is on disk and will show the file as modified in your source control, yet the automatic change can be reverted safely. If the behavior is impacting your IDE negatively, you can disable it by adding the following in your `.csproj` file:

```xml
<PropertyGroup>
  <UnoDisableVSTargetFrameworksRewrite>true</UnoDisableVSTargetFrameworksRewrite>
</PropertyGroup>
```

Note that we are currently tracking these Visual Studio issues, make sure to upvote them:

- `net8.0-browserwasm` must be first for WebAssembly debugging to work ([Link to the issue on Visual Studio developer community](https://developercommunity.visualstudio.com/t/net80-must-be-first-for-WebAssembly-pub/10643720))
- [WinAppSDK Unpackaged profile cannot be selected properly when a net8.0 mobile target is active](https://developercommunity.visualstudio.com/t/WinAppSDK-Unpackaged-profile-cannot-be-s/10643735)

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

- `*.wasm.cs` (WebAssembly)
- `*.desktop.cs` (Desktop)
- `*.iOS.cs` (iOS)
- `*.tvOS.cs`(tvOS)
- `*.UIKit.cs`, `*.Apple.cs` (iOS & tvOS)
- `*.Android.cs` (Android)
- `*.WinAppSDK.cs` (Windows App SDK)

For class libraries we also provide:

- `*.reference.cs` (Reference only)
- `*.crossruntime.cs` (WebAssembly, Desktop, or Reference)

> [!NOTE]
> For backwards compatibility, using `.skia.cs` is currently equivalent to `.desktop.cs`. This might change in the future, so we recommend using the suffixes above instead.

As discussed above setting `EnableDefaultUnoItems` to false will disable these includes.

> [!TIP]
> When you need to exclude specific files from a particular target framework (such as WebAssembly), you can use a custom MSBuild target:
>
> ```xml
> <Target Name="AdjustAppItemGroups" BeforeTargets="ResolveAssemblyReferences">
>     <ItemGroup Condition="'$(TargetFramework)' == 'net9.0-browserwasm'">
>         <None Remove="Page.xaml"/>
>         <Page Remove="Page.xaml"/>
>     </ItemGroup>
> </Target>
> ```
>
> This approach allows you to selectively remove pages from specific target frameworks while maintaining them in others.

## Apple Privacy Manifest Support

Starting May 1st, 2024, Apple requires the inclusion of a new file, the [Privacy Manifest file](https://developer.apple.com/documentation/bundleresources/privacy_manifest_files) (named `PrivacyInfo.xcprivacy`), in app bundles. This file is crucial for complying with updated privacy regulations.

For projects using the Uno.Sdk (version 5.2 or later), the `Platforms/iOS/PrivacyInfo.xcprivacy` file is automatically integrated within the app bundle. An example of this manifest file can be found in the [Uno.Templates](https://aka.platform.uno/apple-privacy-manifest-sample) repository.

For more information on how to include privacy entries in this file, see the [Microsoft .NET documentation](https://learn.microsoft.com/dotnet/maui/ios/privacy-manifest) on the subject, as well as [Apple's documentation](https://developer.apple.com/documentation/bundleresources/privacy_manifest_files).

> [!NOTE]
> If your application is using the Uno Platform 5.1 or earlier, or is not using the Uno.Sdk, you can include the file using the following:
>
> ```xml
>  <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
>    <BundleResource Include="iOS\PrivacyInfo.xcprivacy" LogicalName="PrivacyInfo.xcprivacy" />
>  </ItemGroup>
> ```
