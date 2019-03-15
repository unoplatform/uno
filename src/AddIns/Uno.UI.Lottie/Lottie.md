# Lottie for Uno

**IMPORTANT**
This is an experimental implementation, **only Wasm target is supported yet and still incompleted**.

## Using the `LottieVisualSource`:

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

On Wasm, you'll need the following packages:
  * `Uno.UI.Lottie` (for the `LottieVisualSource`)
  * `Uno.UI` (for the `AnimatedVisualPlayer`)
