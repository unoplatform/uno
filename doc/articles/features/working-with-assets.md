---
uid: Uno.Features.Assets
---

# Assets and image display

Uno Platform automatically processes assets from your app's **Class Library Project** and makes them available on all platforms.

Support for automatic generation of assets multiple resolutions from SVG or PNG is also provided using [Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted).

Platform-specific assets such as [`BundleResource`](https://learn.microsoft.com/xamarin/ios/user-interface/controls/image) and [`AndroidAssets`](https://learn.microsoft.com/xamarin/android/app-fundamentals/resources-in-android/android-assets) are also supported on the heads, when required.

## Supported asset types

At the moment, the following image file types are supported as `Content` assets:

|             | `.bmp` (Win BMP) | `.gif`‡ | `.heic` (Apple) | `.jpg` & `.jpeg` (JFIF) | `.png` | `.webp` | `.pdf` | `.svg` |
| ----------- | :-------------: | :---: | :-----------: | :-----------------: | :--: | :---: | :--: | :--: |
| WinUI       |       ✔️        |   ✔️   |       ❌       |          ✔️          |  ✔️   |   ✔️   |  ❌   |  ✔️   |
| Android 10  |       ✔️        |  ✔️‡   |       ✔️       |          ✔️          |  ✔️   |   ✔️   |  ✔️   |  ✔️   |
| iOS 13      |       ✔️        |  ✔️‡   |       ✔️       |          ✔️          |  ✔️   |   ❌   |  ❌   |  ✔️   |
| macOS       |       ✔️        |  ✔️‡   |       ✔️       |          ✔️          |  ✔️   |   ❌   |  ✔️   |  ❌   |
| WebAssembly†       |       ✔️        |  ✔️‡   |      ❌†       |          ✔️          |  ✔️   |  ❌†   |  ❌†  |  ✔️   |
| Skia WPF    |       ✔️        |  ✔️‡   |       ❌       |          ✔️          |  ✔️   |   ✔️   |  ❌   |  ✔️   |

* † Actual WebAssembly image format support is browser dependent. For example, `.webp` is not working on Safari on macOS, but works on Chromium-based browsers. Check-marks (✔️) indicates a format that can safely expected to work on all browsers able to run Wasm applications.
* ‡ **Gif animation support**:
  * Play/Pause not implemented in Uno yet
  * Always animated on Wasm
  * Not animated on other Uno platforms

## Adding an asset to your project

This is just like adding an asset to a WinUI project, except assets must be added to the shared project to make them available on all platforms.

1. Add an image file to the `Assets` directory of the solution's shared project.
2. Select the item and set the build action to `Content`.

## Referencing an asset

Use assets added to your shared project with the `ms-appx:///` scheme.

See the examples below for XAML:

```xml
<!-- Relative path without a leading '/' uses assets from the library where the XAML is located -->
<Image Source="Assets/MyImage.png" />
<!-- Explicitly qualify the asset, when used from multiple project libraries -->
<Image Source="ms-appx:///[MyApp]/Assets/MyImage.png" />
```

You can also get assets directly using [StorageFile.GetFileFromApplicationUriAsync](xref:Uno.Features.FileManagement#support-for-storagefilegetfilefromapplicationuriasync).

## Qualify an asset

When developing Windows apps, developers can load different assets at runtime based on attributes that qualify their visibility in the app UX. This allows you to tailor your app to different contexts to better suit users' hardware or language preferences.

For instance, such **qualifiers** can selectively load different assets depending on scale, language, theme preferences, etc. This feature is notably useful when supporting high DPI screens or the norms of differing regions.

> [!NOTE]
> To become more familiar with qualifiers, check out Microsoft's documentation for a conceptual overview of the feature:
> [Microsoft: Tailor your resources for language, scale, high contrast, and other qualifiers](https://learn.microsoft.com/windows/apps/windows-app-sdk/mrtcore/tailor-resources-lang-scale-contrast)

Uno Platform allows you to use this same feature on multiple platforms. However, a subset of those qualifiers currently has support.

### Table of scales

Not all scales are supported on all platforms:

| Scale | WinUI       | iOS/MacCatalyst | Android |
|-------|:-----------:|:---------------:|:-------:|
| `100` | scale-100   | @1x             | mdpi    |
| `125` | scale-125   | N/A             | N/A     |
| `150` | scale-150   | N/A             | hdpi    |
| `200` | scale-200   | @2x             | xhdpi   |
| `300` | scale-300   | @3x             | xxhdpi  |
| `400` | scale-400   | N/A             | xxxhdpi |

We recommend including assets for each of these scales: `100`, `150`, `200`, `300`, and `400`. Only compatible scales will be included in each platform.

> [!NOTE]
> In the Android head project (via the csproj), you can set the `UseHighDPIResources` property to `False` in debug. In those cases, only assets with scale `100` (mdpi) and scale `150` (hdpi) will be included. This reduces deployment time when debugging as fewer assets are processed and transferred to the device or simulator.

#### Examples

```paths
\Assets\Images\logo.scale-100.png
\Assets\Images\logo.scale-200.png
\Assets\Images\logo.scale-400.png

\Assets\Images\scale-100\logo.png
\Assets\Images\scale-200\logo.png
\Assets\Images\scale-400\logo.png
```

### Language

Use it as you would on WinUI/UWP, but keep in mind that some language or region combinations might not work on all platforms.

The following languages have been verified to work on all platforms:

-`en`
-`en-US`
-`en-CA`
-`fr`
-`fr-FR`
-`fr-CA`
-`es`

#### Examples

```paths
\Assets\Images\en\logo.png
\Assets\Images\fr\logo.png
\Assets\Images\es\logo.png

\Assets\Images\logo.language-en.png
\Assets\Images\logo.language-fr.png
\Assets\Images\logo.language-es.png
```

### Dark theme support

> [!TIP]  
> Supported on Android only

A theme qualifier can be specified for the image loader to use an asset based on the current app theme.

#### Examples

```paths
/Assets/theme-light/ThemeTestImage.png
/Assets/theme-dark/ThemeTestImage.png
```

### Custom (platform)

Sometimes, you might want to use a different asset depending on the platform. Because there is no `platform` qualifier on WinUI/UWP, Uno Platform provides the `custom` qualifier.

| Platform | Qualifier value |
|----------|-----------------|
| UWP      | `uwp`           |
| iOS      | `ios`           |
| Android  | `android`       |

Because the `custom` qualifier doesn't have any special meaning on WinUI/UWP, we have to interpret its value manually.

On iOS and Android, Uno.UI's `RetargetAssets` task automatically interprets these values and excludes unsupported platforms.

On UWP, you must add the following code to your `App.cs` or `App.xaml.cs` constructor:

```csharp
#if WINDOWS_UWP
    Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("custom", "uwp");
#endif
```

#### Examples

```paths
\Assets\Images\custom-uwp\logo.png
\Assets\Images\custom-ios\logo.png
\Assets\Images\custom-android\logo.png

\Assets\Images\logo.custom-uwp.png
\Assets\Images\logo.custom-ios.png
\Assets\Images\logo.custom-android.png
```

## Android: setting a custom image handler

On Android, to handle the loading of images from a remote url, the Image control has to be provided a
ImageSource.DefaultImageLoader such as the [Android Universal Image Loader](https://github.com/nostra13/Android-Universal-Image-Loader).

This package is installed by default when using the [Uno Cross-Platform solution templates](https://marketplace.visualstudio.com/items?itemName=unoplatform.uno-platform-addin-2022). If not using the solution template, you can install the [nventive.UniversalImageLoader](https://www.nuget.org/packages/nventive.UniversalImageLoader/) NuGet package and call the following code from your application's App constructor:

```csharp
private void ConfigureUniversalImageLoader()
{
    // Create global configuration and initialize ImageLoader with this config
    ImageLoaderConfiguration config = new ImageLoaderConfiguration
        .Builder(Context)
        .Build();

    ImageLoader.Instance.Init(config);

    ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
}
```

## iOS/MacCatalyst: referencing bundle images

On iOS/MacCatalyst, bundle images can be selected using "bundle://" (e.g. bundle:///SplashScreen). When selecting the bundle resource, do not include the zoom factor, nor the file extension.
