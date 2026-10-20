---
uid: Uno.Controls.WebView2
---

# WebView2 (WebView)

> Uno Platform supports two `WebView` controls - the `WebView2` control and the legacy `WebView`. For new development, we strongly recommend `WebView2` as it will get further improvements in the future.

`WebView2` is supported on all Uno Platform targets.

## Basic usage

You can include the `WebView2` control anywhere in XAML:

```xaml
<WebView2 x:Name="MyWebView" Source="https://platform.uno/" />
```

To manipulate the control from C#, first ensure that you call its `EnsureCoreWebView2Async` method:

```csharp
await MyWebView.EnsureCoreWebView2Async();
```

Afterward, you can perform actions such as navigating to an HTML string:

```csharp
MyWebView.NavigateToString("<html><body><p>Hello world!</p></body></html>");
```

## Desktop support

To enable `WebView` on the `-desktop` target, add the `WebView` Uno Feature in your `.csproj`:

```diff
<UnoFeatures>
<!-- Existing features -->
+  WebView;
</UnoFeatures>
```

## WebAssembly support

In case of WebAssembly, the control is supported via a native `<iframe>` element. This means all `<iframe>` browser security considerations and limitations also apply to `WebView`:

- The [`frame-ancestors` Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy/frame-ancestors) can be used to allow embedding a site you have control over, while at the same time blocking third-party sites from embedding
- External site you are embedding must not block embedding via [`X-FRAME-OPTIONS` header](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options)

## Executing JavaScript

When a page is loaded inside the `WebView2` control, you can execute custom JavaScript code. To do this, call the `ExecuteScriptAsync` method:

```csharp
webView.NavigateToString("<div id='test' style='width: 10px; height: 10px; background-color: blue;'></div>");
// Renders a blue <div>

await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor = 'red';");
// The <div> is now red.
```

The method can also return a string result, with returned values being JSON-encoded:

```csharp
await webView.ExecuteScriptAsync("1 + 1"); // Returns a string containing 2
await webView.ExecuteScriptAsync($"(1 + 1).toString()"); // Returns a string containing "2"
await webView.ExecuteScriptAsync("eval({'test': 1})"); // Returns a string containing {"test":1}
```

## JavaScript to C# communication

`WebView2` enables sending web messages from JavaScript to C# on all supported targets. In your web page, include code that sends a message to the `WebView2` control if available. Since Uno Platform runs on multiple targets, you need to use the correct approach for each. We recommend creating a reusable function like the following:

```javascript
function postWebViewMessage(message){
    try{
        if (window.hasOwnProperty("chrome") && typeof chrome.webview !== undefined) {
            // Windows
            chrome.webview.postMessage(message);
        } else if (window.hasOwnProperty("unoWebView")) {
            // Android
            unoWebView.postMessage(JSON.stringify(message));
        } else if (window.hasOwnProperty("webkit") && typeof webkit.messageHandlers !== undefined) {
            // iOS and macOS
            webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(message));
        }
    }
    catch (ex){
        alert("Error occurred: " + ex);
    }
}

// Usage:
postWebViewMessage("hello world");
postWebViewMessage({"some": ['values',"in","json",1]});
```

> **Note:** Make sure not to omit the `JSON.stringify` calls for Android, iOS, and macOS as seen in the snippet above, as they are crucial to transfer data correctly.

On Android, pages also get `window.chrome.webview` (see [Document scripts and two-way messaging](#document-scripts-and-two-way-messaging)), so the first branch of the snippet is taken once it is defined. `unoWebView.postMessage` keeps working.

To receive the message in C#, subscribe to the `WebMessageReceived` event:

```csharp
webView.WebMessageReceived += (s, e) =>
{
    Debug.WriteLine(e.WebMessageAsJson);
};
```

The `WebMessageAsJson` property contains a JSON-encoded string of the data passed to `postWebViewMessage` above.

## Document scripts and two-way messaging

`CoreWebView2` also exposes the WebView2 members used to bootstrap a page and talk to it in both directions, which hosts such as Blazor Hybrid rely on:

- `AddScriptToExecuteOnDocumentCreatedAsync(string)` registers a script that runs in every new document, before the document's own scripts, and returns an ID that `RemoveScriptToExecuteOnDocumentCreated(string)` accepts. A script added before the native WebView is created is queued and registered ahead of the first navigation; the returned operation completes once it is registered.
- `PostWebMessageAsString(string)` and `PostWebMessageAsJson(string)` deliver a message to the page's `window.chrome.webview.addEventListener('message', ...)` listeners.
- The `DOMContentLoaded` event is raised when the top-level document has been parsed, before `NavigationCompleted`. Its `NavigationId` matches the one reported by `NavigationStarting` and `NavigationCompleted`.
- `CoreWebView2WebMessageReceivedEventArgs.Source` is the URI of the document that sent a web message.

```csharp
await webView.EnsureCoreWebView2Async();
await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
    "window.chrome.webview.addEventListener('message', e => console.log(e.data));");

webView.CoreWebView2.DOMContentLoaded += (s, e) => webView.CoreWebView2.PostWebMessageAsString("hello");
webView.CoreWebView2.WebMessageReceived += (s, e) => Debug.WriteLine($"{e.Source}: {e.WebMessageAsJson}");
```

These members are currently available on the following platforms:

| Member | Windows (Skia Win32) | WinAppSDK | Android | macOS, iOS, Linux, WebAssembly |
|--------|----------------------|-----------|---------|--------------------------------|
| `AddScriptToExecuteOnDocumentCreatedAsync` / `RemoveScriptToExecuteOnDocumentCreated` | Yes | Yes | Yes, with limitations (see below) | Not implemented (throws `NotImplementedException`) |
| `PostWebMessageAsString` / `PostWebMessageAsJson` | Yes | Yes | Yes | Not implemented (reported, no effect) |
| `DOMContentLoaded` | Yes | Yes | Yes, with limitations (see below) | Not implemented (never raised) |
| `CoreWebView2WebMessageReceivedEventArgs.Source` | Yes | Yes | The WebView's current URL (see below) | Not implemented (throws `NotImplementedException`) |

On platforms where a member is not implemented, a script queued before the native WebView exists makes the operation fail with `NotImplementedException` once the native WebView is created.

### Android specifics

The Android `WebView` has no API to run a script before a page's own scripts (AndroidX `WebViewCompat.addDocumentStartJavaScript` is not referenced by Uno Platform), so document-created scripts reach a page in one of two ways:

- **Documents the app serves**: a `text/html` main-frame response provided through `WebResourceRequested` (for instance for `https://0.0.0.1/`), and content passed to `NavigateToString`, get the scripts as inline `<script>` elements ahead of the page's own scripts, as on WebView2. The document is re-encoded as UTF-8 to do so. A response with a `Content-Encoding` (such as `gzip`) is served unchanged.
- **Every other document** (pages loaded from the network or from the app package): the scripts run when the page has finished loading, after the page's own scripts. `DOMContentLoaded` is then raised at that time, right before `NavigationCompleted`.

Other limitations:

- Child frames (iframes) don't get document-created scripts or `window.chrome.webview`.
- A page with a Content-Security-Policy that blocks inline scripts gets the scripts after it has loaded instead.
- `WebMessageReceived` is raised for messages sent by the top-level document's `window.chrome.webview.postMessage`. `Source` is the WebView's URL when the message is delivered, since Android does not report the sender's URL. Direct `unoWebView.postMessage` calls are still delivered, but Android exposes `unoWebView` to every frame and origin, so they may come from a child frame.
- `NavigationCompleted` is raised once the page has finished loading (Android's `onPageFinished`).

## Navigating to web content in the application package

To load local web content bundled with the application, you can use the `SetVirtualHostNameToFolderMapping` method. This allows you to set a virtual hostname that maps to a folder within the package, from which the web content will be loaded:

```csharp
await webView.EnsureCoreWebView2Async();
webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
    "UnoNativeAssets",
    "WebContent",
    CoreWebView2HostResourceAccessKind.Allow);
webView.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");
```

This will navigate to the `index.html` file stored in the `WebContent` folder. This folder must be included in a platform-specific location on each platform:

- On Windows, it should be directly in the root of the `YourApp.Windows` project and all its contents should be set to `Content` build action
- On iOS, it should be inside the `Resources` folder and all its contents should be set to `BundleResource` build action
- On Android, it should be inside the `Assets` folder and all its contents should be set to `AndroidAsset` build action

To avoid duplication, you can put the files in a non-project-specific location and add them via linking, e.g.:

```xml
<BundleResource Include="..\LinkedFiles\WebContent\css\site.css" Link="iOS\Resources\WebContent\css\site.css" />
```

The web files can reference each other in a relative path fashion, for example, the following HTML file:

```html
<html>
<head>
    <script src="js/site.js" type="text/javascript"></script>
</head>
<body>
    ...
</body>
</html>
```

Is referencing a `site.js` file inside the `js` subfolder.

## Enabling native developer tools

Set `Uno.UI.FeatureConfiguration.WebView2.EnableDevTools` during application startup, before any `WebView2` is materialized, to enable the platform-native developer tools for the underlying web engine:

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.WebView2.EnableDevTools = true;
    this.InitializeComponent();
}
```

The flag defaults to `true` in `DEBUG` builds and `false` in `RELEASE` builds.

| Platform | What it enables | How to open |
| ---------- | ----------------- | ------------- |
| **Windows / Linux (Skia)** | Chromium DevTools | Right-click inside the WebView and choose **Inspect**, or press <kbd>F12</kbd>. |
| **iOS / macOS** | Safari Web Inspector against the `WKWebView` (iOS 16.4+, macOS 13.3+) | In Safari, enable the **Develop** menu, then pick the device → page. See Apple's [Inspecting iOS](https://developer.apple.com/documentation/safari-developer-tools/inspecting-ios) guide. |
| **Android** | Chrome DevTools remote debugging | Open `chrome://inspect` in desktop Chrome with the device connected. |
| **WebAssembly** | N/A | Use the host browser's developer tools (<kbd>F12</kbd>). |

> [!IMPORTANT]
> On Apple platforms the OS gates inspection to apps signed with the get-task-allow entitlement (DEBUG / development builds). Setting the flag in a RELEASE build has no visible effect.

## Customizing the WebView2 environment (Windows)

On Windows (Skia Desktop) the `WebView2` is backed by the Microsoft Edge WebView2 runtime. A couple of environment-level options can be configured through `Uno.UI.FeatureConfiguration.WebView2` during application startup, before any `WebView2` is materialized. These are Windows-only and have no effect on other targets or on the Windows App SDK target (use `CoreWebView2EnvironmentOptions` directly there).

### Single sign-on with the OS primary account

Set `AllowSingleSignOnUsingOSPrimaryAccount` to `true` to let the `WebView2` use the OS primary account (for example, the Microsoft Entra ID / Azure AD account the user is signed into Windows with) for single sign-on against supporting resources:

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount = true;
    this.InitializeComponent();
}
```

> [!NOTE]
> In a heavily managed environment the flag is necessary but may not be sufficient: device-registration state and administrator policy can still gate Entra ID SSO. Confirm with the environment's administrators that WebView2 AAD SSO is permitted.

### Additional browser arguments

Set `AdditionalBrowserArguments` to pass extra command-line switches (such as proxy configuration or Chromium feature flags) to the underlying browser process, which is often required in locked-down environments:

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.WebView2.AdditionalBrowserArguments = "--proxy-server=http://proxy.example:8080";
    this.InitializeComponent();
}
```

## Creating a CoreWebView2Environment

`CoreWebView2Environment.CreateAsync()` and `CreateWithOptionsAsync(browserExecutableFolder, userDataFolder, options)` are available on every target. The returned environment is *detached*: it records its inputs but does not start a browser. Its `UserDataFolder` returns the folder it was created with (or an empty string), and its `BrowserVersionString` returns the installed browser version where the platform can report one (Windows), or an empty string. `FailureReportFolderPath` and `GetProcessInfos()` need a running WebView and keep throwing `NotImplementedException` on a detached environment.

On Windows (Skia Desktop) the installed runtime is probed first, so a missing WebView2 Runtime fails the call with `FileNotFoundException`, as it does on WinAppSDK. Other targets skip the probe.

```csharp
var options = new CoreWebView2EnvironmentOptions();
var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, options);

await webView.EnsureCoreWebView2Async(environment);
// webView.CoreWebView2.Environment now returns the same instance.
```

`CoreWebView2EnvironmentOptions` stores its values with the WebView2 defaults, but no target reads them yet: the native WebView is still created as by `EnsureCoreWebView2Async()`. On Windows, configure the environment through `FeatureConfiguration.WebView2` instead (see [above](#customizing-the-webview2-environment-windows)).

`environment.CreateWebResourceResponse(content, statusCode, reasonPhrase, headers)` builds a response for `WebResourceRequested`. `headers` is a CRLF-separated list of `Name: Value` lines, as in WebView2. Assigning such a response to `args.Response` is currently honored on Windows (Skia Desktop) and Android.

## Querying the environment and the profile (Windows)

On Windows (Skia Desktop) a subset of `CoreWebView2Environment` and `CoreWebView2Profile` is implemented. On every other target these members throw `NotImplementedException`, as they did before.

> [!NOTE]
> This requires .NET 10 or later. An app targeting .NET 9 keeps the previous `NotImplementedException` behavior for all of the members below.

### Which browser is installed

`CoreWebView2Environment.GetAvailableBrowserVersionString` and `CompareBrowserVersionString` are static and need no `WebView2` at all, so they can be used as a preflight check before showing any web content:

```csharp
try
{
    var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
    var isRecentEnough = CoreWebView2Environment.CompareBrowserVersionString(version, "110.0.0.0") >= 0;
}
catch (FileNotFoundException)
{
    // No WebView2 Runtime is installed.
}
```

Passing a `browserExecutableFolder` is authoritative: if no browser is found there, the call fails rather than falling back to the installed one. The version string carries a channel suffix on non-stable channels (for example `120.0.2210.91 beta`), so parse only the leading token. Note that these are static members, resolved through the Skia host — call them after the application host has started.

The overload taking `CoreWebView2EnvironmentOptions` behaves like the one without it: the options do not change which browser is found.

### Environment and profile of a running WebView2

`CoreWebView2.Environment`, `CoreWebView2.Profile` and `CoreWebView2.BrowserProcessId` become available once the WebView is initialized:

```csharp
await webView.EnsureCoreWebView2Async();

var environment = webView.CoreWebView2.Environment;
// environment.BrowserVersionString, environment.UserDataFolder, environment.FailureReportFolderPath
// environment.GetProcessInfos() returns a snapshot of the browser, renderer and GPU processes.

var profile = webView.CoreWebView2.Profile;
// profile.ProfileName, profile.ProfilePath, profile.IsInPrivateModeEnabled,
// profile.DefaultDownloadFolderPath, profile.PreferredColorScheme
```

`FailureReportFolderPath` is created lazily by the browser, so the directory may not exist yet.

> [!NOTE]
> `ProfileName` is currently always empty on Windows. Uno creates the WebView without controller options, so no profile name is requested — the profile still resolves to the default one, and `ProfilePath` (a directory under `UserDataFolder`) is the reliable way to identify it.

### Clearing browsing data

`ClearBrowsingDataAsync` removes stored state for the profile, which is what a sign-out flow usually needs:

```csharp
// Everything the profile holds.
await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();

// Just cookies. AllSite and AllProfile include cookies too.
await webView.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.Cookies);

// Only what was created in the last hour.
await webView.CoreWebView2.Profile.ClearBrowsingDataAsync(
    CoreWebView2BrowsingDataKinds.Cookies,
    DateTimeOffset.UtcNow.AddHours(-1),
    DateTimeOffset.UtcNow);
```

Clearing a large profile can take several seconds and the displayed content may reload underneath. `CoreWebView2CookieManager` is not implemented, so reading cookies or deleting an individual cookie by name is not available — only bulk clearing.

## Linux specifics

In order to use WebView2 on Linux, you'll need to install `libwebkit2gtk` and `libgtk3-0`:

- On Ubuntu 22.04:

  ```bash
  sudo apt install libwebkit2gtk-4.0-37
  ```

- On Ubuntu 24.04:

  ```bash
  sudo apt install libgtk-3-0 libwebkit2gtk-4.1-dev
  ```

It's overall preferable to use libwebkit2gtk 4.1 whenever possible in order to get http headers support, if your environment allows for it.

### Wayland support

When running on a Wayland environment, the `WebView` control requires the environment variable `GDK_BACKEND` to be set to `x11` to function correctly.

```bash
export GDK_BACKEND=x11
dotnet run
```

## WebResourceRequested

The `WebResourceRequested` event allows you to intercept and modify HTTP requests made by the WebView. This is useful for scenarios like injecting custom headers, implementing authentication, or modifying request/response content.

### Basic usage

To use `WebResourceRequested`, you must first add a filter specifying which URLs should trigger the event, then subscribe to the event:

```csharp
await webView.EnsureCoreWebView2Async();

// Add a filter for all requests
webView.CoreWebView2.AddWebResourceRequestedFilter(
    "*", 
    CoreWebView2WebResourceContext.All,
    CoreWebView2WebResourceRequestSourceKinds.All);

// Subscribe to the event
webView.CoreWebView2.WebResourceRequested += (sender, args) =>
{
    // Access request information
    var uri = args.Request.Uri;
    var method = args.Request.Method;
    
    // Modify headers
    args.Request.Headers.SetHeader("Authorization", "Bearer my-token");
    args.Request.Headers.SetHeader("X-Custom-Header", "custom-value");
    
    // Optionally provide a custom response
    // args.Response = environment.CreateWebResourceResponse(content, 200, "OK", "Content-Type: text/html");
};
```

### Filter parameters

The `AddWebResourceRequestedFilter` method accepts three parameters:

- **uri**: A URI pattern with wildcard support (e.g., `"*"` for all URLs, `"https://api.example.com/*"` for specific domains)
- **resourceContext**: The type of resource to filter (`All`, `Document`, `Image`, `Script`, etc.)
- **requestSourceKinds**: The source of requests to filter (`All`, `Document`, etc.)

### Platform limitations

> [!IMPORTANT]
> `WebResourceRequested` has significant platform-specific limitations. Review the table below to understand what is supported on each platform.

| Platform | Support Level | Header Read | Header Modify | Custom Response | Notes |
| ---------- | -------------- | ------------- | --------------- | ----------------- | ------- |
| **Windows (Win32/WinAppSDK)** | ✅ Full | ✅ | ✅ | ✅ | Full WebView2 support |
| **Android** | ⚠️ Partial | ✅ | ⚠️ | ✅ | Every request can be answered, including virtual hosts such as `https://0.0.0.1/`, and deferrals are honored. Redirect (3xx) responses and request bodies are not supported. Header modification requires re-fetching the resource with HttpClient (only safe for GET/HEAD requests). Session cookies are automatically synchronized. |
| **iOS** | ⚠️ Limited | ✅ | ⚠️ | ❌ | Navigation request headers cannot be modified. However, JavaScript-initiated requests (`fetch`/`XMLHttpRequest`) support custom header injection. Only fires for main document navigation, not sub-resources. |
| **macOS** | ⚠️ Limited | ✅ | ⚠️ | ❌ | Header injection is supported for new requests only. Cannot modify existing request headers. |
| **WebAssembly** | ⚠️ Limited | ✅ | ⚠️ | ❌ | Only `fetch`/`XMLHttpRequest` requests can be intercepted. Standard HTML elements (`img`, `script`, `link`, etc.) cannot have headers modified. Same-origin policy and CORS restrictions apply. May miss requests made during initial page load. |
| **Linux (X11)** | ❌ None | ❌ | ❌ | ❌ | Not implemented. |

### Platform-specific behavior

#### iOS/macOS (WKWebView)

The implementation uses two mechanisms:

1. **Navigation interception**: Fires `WebResourceRequested` for main document navigation (read-only headers)
2. **JavaScript injection**: Automatically injects a script that overrides `window.fetch()` and `XMLHttpRequest.prototype` to apply custom headers to AJAX requests

This means you can inject authentication tokens into API calls made via JavaScript:

```csharp
webView.CoreWebView2.WebResourceRequested += (sender, args) =>
{
    // This will be applied to fetch() and XMLHttpRequest calls
    args.Request.Headers.SetHeader("Authorization", "Bearer my-token");
};
```

#### Android

The event is raised on the UI thread while the WebView waits, so a handler can take a deferral (`args.GetDeferral()`), `await`, and set `args.Response` afterwards. The WebView waits up to 30 seconds; after that, or when no handler sets a response, it loads the request itself.

- Any response from `CoreWebView2Environment.CreateWebResourceResponse` is accepted. Its MIME type and charset come from its `Content-Type` header (or the URL's extension when there is none). Android cannot serve redirect (3xx) responses: those requests are loaded by the WebView itself.
- `args.Request.Content` is always `null`: Android doesn't expose request bodies.
- `ResourceContext` is `Document` for main-frame requests. For other requests it is inferred from the `Sec-Fetch-Dest` and `Accept` headers, then from the URL's extension, so filters on a resource context other than `All` are best-effort. `RequestedSourceKind` is always `Document`.

When headers are modified, the resource is re-fetched using `HttpClient`. The implementation includes:

- **Cookie synchronization**: Session cookies from the WebView are automatically included in re-fetched requests
- **Set-Cookie handling**: Response cookies are synchronized back to the WebView's `CookieManager`

This ensures authenticated sessions work correctly when using `WebResourceRequested`.

#### WebAssembly

For HTML element requests that cannot be intercepted:

- Use Service Workers for more comprehensive request interception
- Proxy requests through your server
- Use JavaScript-based loading for resources that need custom headers

## Accessing the underlying native control

In some advanced scenarios, you may need to access the platform-specific native web view control directly — for example, to configure settings not exposed by the Uno Platform abstraction.

The `WebView2` control template contains a single `ContentPresenter` named `WebViewTemplateRoot`. Each platform sets the `Content` of this presenter to its native web view control. You can retrieve it using `VisualTreeHelper`:

```csharp
await myWebView.EnsureCoreWebView2Async();

var presenter = (ContentPresenter)VisualTreeHelper.GetChild(myWebView, 0);
var nativeControl = presenter.Content;
```

The type of `nativeControl` varies per platform:

| Platform | Native Control Type | Notes |
| ---------- | ------------------- | ------- |
| **Android** | `Android.Webkit.WebView` | Standard Android WebView |
| **iOS** | `WebKit.WKWebView` | Via `UnoWKWebView`, which extends `WKWebView` |
| **macOS (Skia)** | `MacOSNativeWebView` | Internal wrapper using native WebKit via P/Invoke |
| **Windows (Win32/Skia)** | N/A | Uses a native HWND; not directly accessible via `Content` |
| **Linux (X11)** | N/A | Uses a GTK `WebKit.WebView` hosted in a separate window |
| **WebAssembly** | `BrowserHtmlElement` | An HTML `<iframe>` element |

### Example: Configuring the native Android WebView

```csharp
#if __ANDROID__
await myWebView.EnsureCoreWebView2Async();

var presenter = (ContentPresenter)VisualTreeHelper.GetChild(myWebView, 0);

if (presenter.Content is Android.Webkit.WebView androidWebView)
{
    // Access native Android WebView settings
    androidWebView.Settings.BuiltInZoomControls = true;
    androidWebView.Settings.DisplayZoomControls = false;
}
#endif
```

### Example: Configuring the native iOS WKWebView

```csharp
#if __IOS__
await myWebView.EnsureCoreWebView2Async();

var presenter = (ContentPresenter)VisualTreeHelper.GetChild(myWebView, 0);

if (presenter.Content is WebKit.WKWebView wkWebView)
{
    // Access native WKWebView configuration
    wkWebView.AllowsBackForwardNavigationGestures = true;
}
#endif
```

> [!NOTE]
> The native control is only available after calling `EnsureCoreWebView2Async()` and the control template has been applied. The internal types and access patterns may change in future releases.

## WinAppSDK Specifics

When using the WebView2 and running on WinAppSDK, make sure to create an `x64` or `ARM64` configuration:

- In the Visual Studio configuration manager, create an `x64` or `ARM64` solution configuration
- Assign it to the Uno Platform project
- Debug your application using the configuration relevant to your current environment

## Windows Specifics

`WebView2Aot` is the only backend on Windows, and is required in order to use WebView2 with [Native AOT](xref:Uno.Features.NativeAOT). Uno 6.7 also shipped a `Microsoft.Web.WebView2` backend, selectable through the `UNO_WEBVIEW2_BACKEND` environment variable; that backend was removed in Uno 7.0 — `WebView2Aot` is used regardless of the variable's value, and setting it to anything other than `webview2aot` logs a warning instead of switching backends.

If you encounter issues with the WebView2 control on Windows, please file an issue.
