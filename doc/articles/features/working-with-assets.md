---
uid: Uno.Features.Assets
---

# Assets and image display

Uno Platform automatically processes assets from your app's project and makes them available on all platforms.

Support for automatically generating assets in multiple resolutions from SVG or PNG is also provided using [Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted).

Platform-specific assets such as [`BundleResource`](https://learn.microsoft.com/xamarin/ios/user-interface/controls/image) and [`AndroidAssets`](https://learn.microsoft.com/xamarin/android/app-fundamentals/resources-in-android/android-assets) are also supported on the heads, when required.

## Supported asset types

The following image file types are supported as `Content` assets:

|                                    | `.bmp` (Win BMP) | `.gif` | `.heic` (Apple) | `.jpg` & `.jpeg` (JFIF) | `.png` | `.webp` | `.pdf` | `.svg` |
| ---------------------------------- | :--------------: | :----: | :-------------: | :---------------------: | :----: | :-----: | :----: | :----: |
| WinUI                              |        ✔️        |   ✔️   |       ❌        |           ✔️            |   ✔️   |   ✔️    |   ❌   |   ✔️   |
| Skia renderer¹                     |        ✔️        |  ✔️²   |       ❌³       |           ✔️            |   ✔️   |   ✔️²   |   ❌   |  ✔️⁴   |
| Android native renderer (6.x)¹     |       ❌⁵        |  ✔️⁶   |       ❌⁵       |           ✔️            |   ✔️   |   ❌⁵   |   ❌   |  ✔️⁴   |
| iOS/tvOS native renderer (6.x)¹    |        ✔️        |  ✔️⁶   |       ✔️        |           ✔️            |   ✔️   |   ✔️⁶   |   ❌   |  ✔️⁴   |
| WebAssembly native renderer (6.x)¹ |        ✔️        |  ✔️²   |       ❌³       |           ✔️            |   ✔️   |   ✔️²   |  ❌³   |   ✔️   |

1. Every target uses the Skia renderer from Uno Platform 7.0. In 6.x, Desktop always does, and Android, iOS/tvOS and WebAssembly do when `UnoFeatures` includes `SkiaRenderer` (the default for new projects). Without it, they use the native renderers.
2. Animated images always play. `BitmapImage.AutoPlay`, `IsPlaying`, `Play()` and `Stop()` are not implemented.
3. Depends on the browser on WebAssembly. For example, Safari decodes `.heic` and Chrome doesn't.
4. Uno Platform 6.x needs the `Svg` Uno feature. See [Using SVG images](xref:Uno.Features.SVG).
5. Doesn't load as an `ms-appx:///` asset, because the native Android renderer only packages `.png`, `.jpg`, `.jpeg` and `.gif` files as images. It loads from a stream or a web URL.
6. Animated images show only the first frame.

## Adding an asset to your project

This is just like adding an asset to a WinUI project.

1. Add an image file to the `Assets` directory of the solution's app project.
2. Select the item and set the build action to `Content`, if not already set.

## Referencing an asset

Use assets added to your project with the `ms-appx:///` scheme.

See the examples below for XAML:

```xml
<!-- Relative path without a leading '/' uses assets from the library where the XAML is located -->
<Image Source="Assets/MyImage.png" />
<!-- Explicitly qualify the asset, when used from multiple project libraries -->
<Image Source="ms-appx:///[MyApp]/Assets/MyImage.png" />
```

> [!NOTE]
> For [single project solutions](https://platform.uno/docs/articles/migrating-to-single-project.html#project-structure), the `[MyApp]` qualifier is not required when explicitly qualifying the asset.

```xml
<Image Source="ms-appx:///Assets/MyImage.png" />
```

Or

```xml
<Image Source="ms-appx:///Assets/Images/MyImage.png" />
```

For Uno's latest project template where images are saved in the `Images` folder.

### Referencing an asset from code

You can also reference assets from code using the `ms-appx:///` scheme. For example:

```xaml
<Image x:Name="MyImage" />
```

```csharp
// from XAML code behind
MyImage.Source = "ms-appx:///Assets/MyImage.png";
```

or using bindings:

```csharp
// from a ViewModel
public string ImagePath { get; set ;} = "ms-appx:///Assets/MyImage.png";
```

```xaml
<Image Source="{x:Bind ImagePath}" />
```

You can also get assets directly using [StorageFile.GetFileFromApplicationUriAsync](xref:Uno.Features.FileManagement#support-for-storagefilegetfilefromapplicationuriasync).

## Qualify an asset

When developing Windows apps, developers can load different assets at runtime based on attributes that qualify their visibility in the app's UX. This allows you to tailor your app to different contexts to better suit users' hardware or language preferences.

For instance, such **qualifiers** can selectively load different assets depending on scale, language, theme preferences, etc. This feature is notably useful when supporting high DPI screens or the norms of differing regions.

> [!NOTE]
> To become more familiar with qualifiers, check out Microsoft's documentation for a conceptual overview of the feature:
> [Microsoft: Tailor your resources for language, scale, high contrast, and other qualifiers](https://learn.microsoft.com/windows/apps/windows-app-sdk/mrtcore/tailor-resources-lang-scale-contrast)

Uno Platform allows you to use the same feature on multiple platforms. However, only a subset of those qualifiers currently have support.

### Table of scales

Not all scales are supported on all platforms:

| Scale | WinUI       | iOS/tvOS | Android |
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

Sometimes, you might want to use a different asset depending on the platform. Because there is no `platform` qualifier on WinUI, Uno Platform provides the `custom` qualifier. Uno.UI's `RetargetAssets` task interprets it on Android, iOS, tvOS and Mac Catalyst heads and excludes the variants that don't match the platform. Uno Platform doesn't interpret it on other heads.

| Platform                | Qualifier value |
|-------------------------|-----------------|
| iOS, tvOS, Mac Catalyst | `ios`           |
| Android                 | `android`       |

#### Examples

```paths
\Assets\Images\custom-ios\logo.png
\Assets\Images\custom-android\logo.png

\Assets\Images\logo.custom-ios.png
\Assets\Images\logo.custom-android.png
```

## Android native renderer: setting a custom image handler

With the native Android renderer (`UnoFeatures` without `SkiaRenderer`), the `Image` control needs an `ImageSource.DefaultImageLoader`, such as the [Android Universal Image Loader](https://github.com/nostra13/Android-Universal-Image-Loader), to load images from a remote URL. The Skia renderer doesn't use it.

The Uno.Sdk references the `Uno.UniversalImageLoader` package for native Android heads, and the app template configures it in `Platforms/Android/Main.Android.cs`. Otherwise, call the following code from your Android `Application` constructor:

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
