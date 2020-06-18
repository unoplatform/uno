# ProgressRing

There are 2 progress rings available in Uno:

* `Windows.UI.Xaml.Controls.ProgressRing` - the UWP one, support for both native & UWP styling.
* `Microsoft.UI.Xaml.Controls.ProgressRing` - the new version, which is powered by Lottie.

## Using the legacy `Windows.UI.Xaml.Controls.ProgressRing`

This control works on all platforms and uses the native progress ring control by default, with the exception of Wasm where there is no native progress ring control.

## Using the new `Microsoft.UI.Xaml.Controls.ProgressRing`

This version comes with [WinUI 2.4](https://docs.microsoft.com/en-us/windows/apps/winui/winui2/release-notes/winui-2.4#progressring) and is using an `<AnimatedVisualPlayer />` in its Control Template. It is also designed to be a replacement for the legacy version, where a custom template should work unchanged with this control.

**IMPORTANT**: To use the refreshed visual style, you must add a reference to `Uno.UI.Lottie` package to your projects or the ring will not be displayed.







