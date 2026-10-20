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
`CoreWebView2`. However, the actual platform-specific logic is then in yet another layer - classes that implement
the `INativeWebView` interface. This is the actual place where the platform-bound `WebViews` reside.

Most of the native logic is provided by the native web view controls - e.g. `WKWebView` on iOS, `WebView` on Android.

## Navigation event order

Both `WebView` and `WebView2` have the same navigation event order:

- `NavigationStarting`
- `ContentLoading`
- `DOMContentLoaded`

At the end of the navigation, on `WebView` the either `NavigationCompleted` or `NavigationFailed` are raised.
`WebView2` has only `NavigationCompleted`.

In `WebView`, most the events include `Uri`, but `WebView2` uses a unique `NavigationId` instead.
For details on `WebView2` event behavior see: https://learn.microsoft.com/microsoft-edge/webview2/concepts/navigation-events

## `chrome.webview` bridge for native views without WebView2

Only Win32 hosts the real WebView2. Native views that lack `chrome.webview` (WebKit on macOS/iOS, Android `WebView`,
WebKitGTK on X11) can build the WebView2 scripting surface from shared pieces in `Native/`:

- `WebViewMessageBridge` generates the script that defines `window.chrome.webview` (`postMessage`,
  `addEventListener`/`removeEventListener`) over a platform channel such as
  `window.webkit.messageHandlers.unoWebView.postMessage`. Page messages arrive as `JSON.stringify(value)`, ready for
  `RaiseWebMessageReceived`. The bridge also reports `DOMContentLoaded` with a reserved `@@uno-webview/lifecycle/`
  message, which platforms filter with `TryParseLifecycle`.
- `ScriptBridgeWebViewCapabilities` implements `ISupportsDocumentCreatedScripts`, `ISupportsPostWebMessage` and
  `ISupportsDOMContentLoaded` on top of document-start scripts and script evaluation, with fallbacks for views that
  can only rewrite the HTML (`BeforeParse`) or run scripts after load (`Late`).
- `WebViewVirtualHostMap` and `ISupportsCustomSchemeVirtualHosts` let a view that cannot intercept `https` serve
  virtual hosts (`SetVirtualHostNameToFolderMapping` hosts and `0.0.0.0/8`, which Blazor uses) through the
  `uno-app` scheme. `CoreWebView2` translates URLs at its boundary, so apps only see `https://`.
- `WebResourceResponseSnapshot` reads any `WebResourceRequested` response into status, headers and bytes.
- `HtmlDocumentScriptInjector` applies `BeforeParse` injection to a response body held as bytes (charset sniffing,
  re-encoding as UTF-8). Android uses it for the `text/html` documents it serves through `WebResourceRequested`, and
  falls back to `Late` injection for the documents it does not serve.

The node script `src/Uno.UI.UnitTests/Windows_UI_XAML_Controls/WebView2Tests/WebViewMessageBridge.node.js` checks the
bridge script against a fake `window`; run it with `node` after changing the script.
