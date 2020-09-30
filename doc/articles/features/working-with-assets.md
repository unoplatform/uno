# Assets and image display

In a standard Xamarin project, you must duplicate, rename, and manually add your assets to each target project (UWP, iOS, Android). 

[Xamarin: Working with Images](https://developer.xamarin.com/samples/xamarin-forms/WorkingWithImages/)

We still recommend using the above technique for platform-specific icons and splash screens in Uno.UI projects.

For most other assets, Uno.UI uses custom build tasks to lets you include assets once in shared projects and automatically use them on all platforms. The rest of this document will cover those particular features.

## Supported asset types

At the moment, only the following image file types are supported:

|             | .bmp (Win BMP) | .gif‡ | .heic (Apple) | .jpg & .jpeg (JFIF) | .png | .webp | .pdf | .svg |
| ----------- | :------------: | :---: | :-----------: | :-----------------: | :--: | :---: | :--: | :--: |
| Windows UWP |       ✔️        |   ✔️   |       ❌       |          ✔️          |  ✔️   |   ✔️   |  ❌   |  ✔️   |
| Android 10  |       ✔️        |  ✔️‡   |       ✔️       |          ✔️          |  ✔️   |   ✔️   |  ✔️   |  ✔️   |
| iOS 13      |       ✔️        |  ✔️‡   |       ✔️       |          ✔️          |  ✔️   |   ❌   |  ❌   |  ❌   |
| macOS       |       ✔️        |  ✔️‡   |       ✔️       |          ✔️          |  ✔️   |   ❌   |  ✔️   |  ❌   |
| Wasm†       |       ✔️        |  ✔️‡   |      ❌†       |          ✔️          |  ✔️   |  ❌†   |  ❌†  |  ✔️   |
| Skia WPF    |       ✔️        |  ✔️‡   |       ❌       |          ✔️          |  ✔️   |   ✔️   |  ❌   |  ❌   |

* † Actual **Wasm image format support** is browser dependent. For example, `.webp` is not working on Safari on macOS, but works on Chromium-based browsers. Checkmarks (✔️) indicates a format that can safely expected to work on all browsers able to run Wasm applications.
* ‡ **Gif animation support**:
  * Play/Pause not implemented in Uno yet
  * Always animated on Wasm
  * Not animated on other Uno platforms

## Adding an asset to your project

This is just like adding an asset to any UWP project. Just make sure to add the asset to a shared project to make it available to all platforms.

1. Add the image file to the `Assets` directory of a shared project.
2. Set the build action to `Content`.

## Qualify an asset

On UWP, you can use qualifiers to load different assets depending on scale, language, etc. 

[Microsoft: Tailor your resources for language, scale, high contrast, and other qualifiers](https://docs.microsoft.com/en-us/windows/uwp/app-resources/tailor-resources-lang-scale-contrast)

You can do the same thing with Uno.UI, although only a subset of those qualifiers are supported.

### Scale

Not all scales are supported on all platforms:

| Scale | UWP         | iOS      | Android |
|-------|:-----------:|:--------:|:-------:|
| `100` | scale-100   | @1x      | mdpi    |
| `125` | scale-125   | N/A      | N/A     |
| `150` | scale-150   | N/A      | hdpi    |
| `200` | scale-200   | @2x      | xhdpi   |
| `300` | scale-300   | @3x      | xxhdpi  |
| `400` | scale-400   | N/A      | xxxhdpi |

We recommend including assets for each of these scales: `100`, `150`, `200`, `300` and `400`. Only compatible scales will be included to each platform.

*Note: In the Android head project (via the csproj), you can set the `UseHighDPIResources` property to `False` in debug. In those cases, only assets with scale `100` (mdpi) and scale `150` (hdpi) will be included. This reduces deployment time when debugging as fewer assets are processed and transferred to the device or simulator.*

#### Examples

```
\Assets\Images\logo.scale-100.png
\Assets\Images\logo.scale-200.png
\Assets\Images\logo.scale-400.png

\Assets\Images\scale-100\logo.png
\Assets\Images\scale-200\logo.png
\Assets\Images\scale-400\logo.png
```

### Language

Use it as you would on UWP, but keep in mind that some language/region combinations might not work on all platforms.

The following languages have been verified to work on all platforms:
- `en`
- `en-US`
- `en-CA`
- `fr`
- `fr-FR`
- `fr-CA`
- `es`

#### Examples

```
\Assets\Images\en\logo.png
\Assets\Images\fr\logo.png
\Assets\Images\es\logo.png

\Assets\Images\logo.language-en.png
\Assets\Images\logo.language-fr.png
\Assets\Images\logo.language-es.png
```

### Dark theme support

> Supported on Android only

A theme qualifier can be specified for the image loader to use an asset based on the current app theme.

#### Examples

```
/Assets/theme-light/ThemeTestImage.png
/Assets/theme-dark/ThemeTestImage.png
```

### Custom (platform)

Sometimes, you might want to use a different asset depending on the platform. Because there is no `platform` qualifier on UWP, we had to use the `custom` qualifier.

| Platform | Qualifier value |
|----------|-----------------|
| UWP      | `uwp`           |
| iOS      | `ios`           |
| Android  | `android`       |

Because the `custom` qualifier doesn't have any special meaning on UWP, we have to interpret its value manually.

On iOS and Android, Uno.UI's `RetargetAssets` task automatically interprets these values and excludes unsupported platforms.

On UWP, you must add the following code to your `App.xaml.cs` constructor:

```csharp
#if WINDOWS_UWP
	Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("custom", "uwp");
#endif
```

#### Examples

```
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

This package is installed by default when using the [Uno Cross-Platform solution templates](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin). If not using the solution template, you can install the [nventive.UniversalImageLoader](https://www.nuget.org/packages/nventive.UniversalImageLoader/) NuGet package and call the following code from your application's App constructor:

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

## iOS: referencing bundle images

On iOS, bundle images can be selected using "bundle://" (e.g. bundle:///SplashScreen). When selecting the bundle resource, do not include the zoom factor, nor the file extension.