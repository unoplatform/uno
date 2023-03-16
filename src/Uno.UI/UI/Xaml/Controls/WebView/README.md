# `WebView` Developer Notes

On UWP, there are two variants of `WebView`:

- `WebView` - the default `WebView` implementation, which uses the EdgeHTML rendering engine.
- `WebView2` - the new `WebView` implementation, which uses the Chromium-based Edge rendering engine.

On WinAppSDK, there is only one variant of `WebView`:

- `WebView2` - the new `WebView` implementation, which uses the Chromium-based Edge rendering engine.

In case of Uno Platform, both `WebView` and `WebView2` are currently provided in both `Uno.UI` and `Uno.WinUI`.
`WebView` may be removed in a future release as a breaking change.

## Anatomy of `WebView` in Uno


