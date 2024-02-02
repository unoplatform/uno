---
uid: Uno.Features.ProgressRing
---

# ProgressRing

<<<<<<< HEAD
<<<<<<< HEAD
There are two implementations of the `ProgressRing` control available in Uno:

Uno Platform provides two versions of the `ProgressRing` control:

* `Windows.UI.Xaml.Controls.ProgressRing` - "WUX `ProgressRing`" - implementation based on the built-in control in Universal Windows Platform, with support for both native & UWP styling.
* `Microsoft.UI.Xaml.Controls.ProgressRing` - "MUX `ProgressRing`", implementation based on WinUI 2.x and WinUI 3 (see [here](https://github.com/microsoft/microsoft-ui-xaml/tree/main/dev/ProgressRing),  powered by Lottie animations.

| Control            | iOS | macOS | Android | WASM | Skia (GTK, WPF, FrameBuffer, Tizen) |
|--------------------|-----|-------|---------|------|-------------------------------------|
| MUX `ProgressRing` | ✔   | ✔     | ✔       | ✔    | ✔                                   |
| WUX `ProgressRing` | ✔   | ✔     | ✔       | ✔    | ✔                                   |

## Using the `Microsoft.UI.Xaml.Controls.ProgressRing`
=======
# [**WinUI**](#tab/winui)
>>>>>>> 79dc6e13ce (docs: Refresh progessring docs)
=======
## [**WinUI**](#tab/winui)
>>>>>>> 85064af654 (docs: Apply suggestions from code review)

![MUX `ProgressRing`](../Assets/features/progressring/muxprogressring.png)

This version comes with [WinUI 2.x and WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui2/release-notes/winui-2.4#progressring) and is using an `<AnimatedVisualPlayer />` in its Control Template to display Lottie-based animations.

> [!IMPORTANT]
> To use this Control, you must add a [reference the Lottie package](xref:Uno.Features.Lottie) in your projects, or the ring will not be displayed.

## [**UWP**](#tab/uwp)

![WUX `ProgressRing`](../Assets/features/progressring/wuxprogressring.png)

This control works on all platforms and uses the native progress ring control by default, with the exception of Wasm where there is no native progress ring control.

> [!NOTE]
> In WinUI-based Uno Platform apps, this control is in the `Uno.UI.Controls.Legacy` namespace instead. It is still available as part of Uno Platform 5.x for its support of native styling.

On Android and iOS, the WUX `ProgressRing` uses native controls by default (`UIActivityIndicatorView` on iOS and `ProgressBar` on Android). To use the UWP rendering on these targets, you can explicitly apply the `DefaultWuxProgressRingStyle` Style:

```xaml
<ProgressRing Style="{StaticResource DefaultWuxProgressRingStyle}" />
```

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

***
