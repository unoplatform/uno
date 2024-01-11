---
uid: Uno.Features.ProgressRing
---

# ProgressRing

There are two implementations of the `ProgressRing` control available in Uno:

Uno Platform provides two versions of the `ProgressRing` control:

* `Windows.UI.Xaml.Controls.ProgressRing` - "WUX `ProgressRing`" - implementation based on the built-in control in Universal Windows Platform, with support for both native & UWP styling.
* `Microsoft.UI.Xaml.Controls.ProgressRing` - "MUX `ProgressRing`", implementation based on WinUI 2.x and WinUI 3 (see [here](https://github.com/microsoft/microsoft-ui-xaml/tree/main/dev/ProgressRing),  powered by Lottie animations.

| Control            | iOS | macOS | Android | WASM | Skia (GTK, WPF, FrameBuffer, Tizen) |
|--------------------|-----|-------|---------|------|-------------------------------------|
| MUX `ProgressRing` | ✔   | ✔     | ✔       | ✔    | ✔                                   |
| WUX `ProgressRing` | ✔   | ✔     | ✔       | ✔    | ✔                                   |

## Using the `Microsoft.UI.Xaml.Controls.ProgressRing`

![MUX `ProgressRing`](../Assets/features/progressring/muxprogressring.png)

This version comes with [WinUI 2.x and WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui2/release-notes/winui-2.4#progressring) and is using an `<AnimatedVisualPlayer />` in its Control Template. It is also designed to be a replacement for the legacy version, where a custom template should work unchanged with this control.

> [!IMPORTANT]
> To use the refreshed visual style, you must [reference the Lottie package](Lottie.md) in your projects, or the ring will not be displayed.

## Using the `Windows.UI.Xaml.Controls.ProgressRing`

![WUX `ProgressRing`](../Assets/features/progressring/wuxprogressring.png)

This control works on all platforms and uses the native progress ring control by default, with the exception of Wasm where there is no native progress ring control.

> [!NOTE]
> In WinUI-based Uno Platform apps, this control is in the `Uno.UI.Controls.Legacy` namespace instead.

### Native styles

On Android and iOS, the WUX `ProgressRing` uses native controls by default (`UIActivityIndicatorView` on iOS and `ProgressBar` on Android). To use the UWP rendering on these targets, you can explicitly apply the `DefaultWuxProgressRingStyle` Style:

```xaml
<ProgressRing Style="{StaticResource DefaultWuxProgressRingStyle}" />
```

## Platform-specific usage

To use the MUX `ProgressRing` on non-Skia targets and WUX `ProgressRing` on Skia targets you can utilize platform-specific XAML syntax:

```xaml
<Page
   ...
   mux="using:Microsoft.UI.Xaml.Controls"
   not_skia="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
   skia="http://uno.ui/skia"
   mc:Ignorable="d skia">
   <Grid>
      <skia:Border>
        <ProgressRing />
      </skia:Border>
      <not_skia:Border>
        <mux:ProgressRing />
      </not_skia:Border>
   </Grid>
</Page>
```
