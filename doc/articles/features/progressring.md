---
uid: Uno.Features.ProgressRing
---

# ProgressRing

# [**WinUI**](#tab/winui)

![MUX `ProgressRing`](../Assets/features/progressring/muxprogressring.png)

This version comes with [WinUI 2.x and WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui2/release-notes/winui-2.4#progressring) and is using an `<AnimatedVisualPlayer />` in its Control Template to display Lottie-based animations.

> [!IMPORTANT]
> To use this Control, you must add a [reference the Lottie package](xref:Uno.Features.Lottie) in your projects, or the ring will not be displayed.

# [**UWP**](#tab/uwp)

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


