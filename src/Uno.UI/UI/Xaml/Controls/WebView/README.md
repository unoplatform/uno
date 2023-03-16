# `WebView` Developer Notes

On UWP, there are two variants of `WebView`:

- `WebView` - the default `WebView` implementation, which uses the EdgeHTML rendering engine.
- `WebView2` - the new `WebView` implementation, which uses the Chromium-based Edge rendering engine.

On WinAppSDK, there is only one variant of `WebView`:

- `WebView2` - the new `WebView` implementation, which uses the Chromium-based Edge rendering engine.

In case of Uno Platform, both `WebView` and `WebView2` are currently provided in both `Uno.UI` and `Uno.WinUI`.
`WebView` may be removed in a future release as a breaking change.

## Structure of `WebView2`

`WebView2` consists of two components - `WebView2` class, which is a Windows App SDK `Control` and a `CoreWebView2`,
which is a "native" controller, shared among different `WebView2` implementations (WPF, WinForms, etc.). The inner
`CoreWebView2` is accessible from `WebView2` via `CoreWebView2` property.

## Anatomy of `WebView` and `WebView2` in Uno

To make the shared implementation between `WebView` and `WebView2` as large as possible, both controls will have
use the `CoreWebView2` for most of the logic. The public members of both controls will then act as a proxy to the
`CoreWebView2` implementations.

## Navigation event order

Both `WebView` and `WebView2` have the same navigation event order:

- `NavigationStarting`
- `ContentLoading`
- `DOMContentLoaded`

At the end of the navigation, on `WebView` the either `NavigationCompleted` or `NavigationFailed` are raised.
`WebView2` has only `NavigationCompleted`.

In `WebView`, most the events include `Uri`, but `WebView2` uses a unique `NavigationId` instead.
