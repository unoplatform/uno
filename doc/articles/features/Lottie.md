# Lottie for Uno

**IMPORTANT**
This is an experimental implementation, **still incomplete**.

## Using the `LottieVisualSource`:

Add the following namespaces:
```xml
<Page
    ...
    xmlns:winui="using:Microsoft.UI.Xaml.Controls"
	xmlns:lottie="using:Microsoft.Toolkit.Uwp.UI.Lottie"
    ...>
```

```xml
<winui:AnimatedVisualPlayer
    x:Name="player"
    AutoPlay="true">

    <lottie:LottieVisualSource
        UriSource="ms-appx:///Lottie/4930-checkbox-animation.json" />
</winui:AnimatedVisualPlayer>
```

Documentation: <https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer>

On UWP, you'll need to reference the following packages in your head project:
  * `Microsoft.Toolki.Uwp.UI.Lottie` (for the `LottieVisualSource`)
  * `Microsoft.UI.Xaml` (for the `AnimatedVisualPlayer`)

On WASM, Android, iOS and macOS, you'll need the following packages:
  * `Uno.UI.Lottie` (for the `LottieVisualSource`)
  * `Uno.UI` (for the `AnimatedVisualPlayer`)

## Lottie JSON file location

On WASM, iOS and macOS, you can put the Lottie .json files directly in a folder of the shared project (for example "Lottie/myanimation.json") and set their Build action as Content.

On Android, Lottie .json files need to be added into the Assets folder. To match the same path as for the other platforms, the file could be stored at "Assets/Lottie/myanimation.json". Set its Build action to AndroidAsset.

To reference the animations in XAML, use the `ms-appx:` URI, in this case `ms-appx:///Lottie/myanimation.json`.

## Using `embedded://` scheme

**WARNING**: Not supported on Windows, it's a Uno-only feature.

You can put the file as `<EmbeddedResource>` in your assembly and retrieve it using the following url format as `UriSource`:

```
embedded://<assemblyname>/<resource name>
```

* You can specify `.` in assembly name to use the Application's assembly.
* You can specify `(assembly)` in path: will be replaced by assembly name.
* AssemblyName is case insensitive, **but the resource name is**.

## Limitations

On Android, the `Stretch` mode of `Fill` is not currently supported.
