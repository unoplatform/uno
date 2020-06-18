# ProgressRing

There is 2 progress rings available in Uno:

* `Windows.UI.Xaml.Controls.ProgressRing` - the default "legacy" one, support for both native & UWP styling.
* `Microsoft.UI.Xaml.Controls.ProgressRing` - the new version, which is powered by Lottie.

## Using the legacy `Windows.UI.Xaml.Controls.ProgressRing`

This should work on all platforms and will use the native progress ring control by default. Except on Wasm where there's no such thing as a native progress ring control.

## Using the new `Microsoft.UI.Xaml.Controls.ProgressRing`

This version come with [WinUI 2.4](https://docs.microsoft.com/en-us/windows/apps/winui/winui2/release-notes/winui-2.4#progressring) and is using an `<AnimatedVisualPlayer />` in its Control Template. It's also designed to be a replacement for the legacy version, so your custom template should work unchanged with this version.

**IMPORTANT**: To use the refreshed visual style, you **MUST** add a reference to `Uno.UI.Lottie` package to your projects or the ring won't show at all.







