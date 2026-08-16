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

To receive the message in C#, subscribe to the `WebMessageReceived` event:

```csharp
webView.WebMessageReceived += (s, e) =>
{
    Debug.WriteLine(e.WebMessageAsJson);
};
```

The `WebMessageAsJson` property contains a JSON-encoded string of the data passed to `postWebViewMessage` above.

## C# to JavaScript communication

Use `PostWebMessageAsString` or `PostWebMessageAsJson` to send a message from C# to the current page:

```csharp
await webView.EnsureCoreWebView2Async();

webView.CoreWebView2.PostWebMessageAsString("hello");
webView.CoreWebView2.PostWebMessageAsJson("""{"command":"refresh"}""");
```

Receive these messages in JavaScript through the WebView2-compatible message event:

```javascript
window.chrome.webview.addEventListener("message", event => {
    console.log(event.data);
});
```

`PostWebMessageAsJson` validates that its argument contains one JSON value. Both methods throw when `CoreWebView2Settings.IsWebMessageEnabled` is `false`.

## Running scripts when a document is created

`AddScriptToExecuteOnDocumentCreatedAsync` registers JavaScript that runs at the start of each subsequent document:

```csharp
var scriptId = await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
    "window.unoHostAvailable = true;");

// Remove the registration when it is no longer needed.
webView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(scriptId);
```

Document-created scripts are not supported by the WebAssembly iframe host.

## WebView settings

The following settings are available through `CoreWebView2.Settings`:

```csharp
await webView.EnsureCoreWebView2Async();

webView.CoreWebView2.Settings.UserAgent = "MyApp/1.0";
webView.CoreWebView2.Settings.IsScriptEnabled = true;
webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
```

Platform browser restrictions still apply. WebAssembly cannot override its user agent or disable scripts and browser zoom. Some WebKit-based hosts retain the requested zoom setting without changing native gesture behavior.

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
| **iOS / Mac Catalyst / macOS** | Safari Web Inspector against the `WKWebView` (iOS 16.4+, macOS 13.3+) | In Safari, enable the **Develop** menu, then pick the device → page. See Apple's [Inspecting iOS](https://developer.apple.com/documentation/safari-developer-tools/inspecting-ios) guide. |
| **Android** | Chrome DevTools remote debugging | Open `chrome://inspect` in desktop Chrome with the device connected. |
| **WebAssembly** | N/A | Use the host browser's developer tools (<kbd>F12</kbd>). |

> [!IMPORTANT]
> On Apple platforms the OS gates inspection to apps signed with the get-task-allow entitlement (DEBUG / development builds). Setting the flag in a RELEASE build has no visible effect.
>
> [!NOTE]
> The legacy iOS-only `Uno.UI.FeatureConfiguration.WebView2.IsInspectable` property is now an obsolete alias for `EnableDevTools`.

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

### Per-control environment and profile options

Use the `EnsureCoreWebView2Async` overloads to initialize a control with a custom environment and controller options:

```csharp
var environmentOptions = new CoreWebView2EnvironmentOptions
{
    AdditionalBrowserArguments = "--disable-features=ExampleFeature",
    Language = "en-US",
};

var userDataFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MyApp",
    "WebViewProfiles",
    "Profile1");

var environment = await CoreWebView2Environment.CreateWithOptionsAsync(
    browserExecutableFolder: null,
    userDataFolder,
    environmentOptions);

var controllerOptions = environment.CreateCoreWebView2ControllerOptions();
controllerOptions.ProfileName = "Profile1";
controllerOptions.IsInPrivateModeEnabled = true;

await webView.EnsureCoreWebView2Async(environment, controllerOptions);
```

Windows supports the complete environment and controller option set on both WebView2 backends. The macOS Skia host supports private mode. Other custom environment combinations throw `NotSupportedException` when the native browser cannot provide equivalent behavior.

## Cookies

Use `CoreWebView2.CookieManager` to create, query, update, and delete cookies:

```csharp
var manager = webView.CoreWebView2.CookieManager;
var cookie = manager.CreateCookie("session", "value", "example.com", "/");
cookie.IsSecure = true;
cookie.IsHttpOnly = true;

manager.AddOrUpdateCookie(cookie);
var cookies = await manager.GetCookiesAsync("https://example.com/");
manager.DeleteCookie(cookie);
```

Windows, Apple platforms, and Android expose their native cookie stores. Android requires an absolute URI when querying cookies and cannot enumerate every cookie in the profile. Cookie management is not available on WebAssembly or the X11 WebKitGTK host.

## Printing

Use `PrintToPdfStreamAsync` to capture the current document as PDF, or `ShowPrintUI` to open the platform print UI:

```csharp
var settings = webView.CoreWebView2.Environment.CreatePrintSettings();
settings.Orientation = CoreWebView2PrintOrientation.Landscape;
settings.ShouldPrintBackgrounds = true;

using var pdf = await webView.CoreWebView2.PrintToPdfStreamAsync(settings);
webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
```

PDF output is supported on Windows, macOS, iOS, and X11. Android and WebAssembly can show print UI but do not provide PDF streams through this API. Some platform print engines support only a subset of `CoreWebView2PrintSettings` and throw `NotSupportedException` for unsupported combinations.

## Lifecycle and cleanup

In addition to navigation events, `CoreWebView2` exposes `ContentLoading`, `DOMContentLoaded`, `DocumentTitleChanged`, `HistoryChanged`, and `SourceChanged`.

The document/content events depend on equivalent callbacks from the native browser backend and may not be available on every target.

Call `WebView2.Close()` when the control will not be used again. Closing releases native browser resources and is terminal: subsequent navigation, script execution, or initialization calls throw `ObjectDisposedException`.

## Querying the environment and the profile (Windows)

Live `CoreWebView2Environment` and `CoreWebView2Profile` metadata is implemented on Windows (Skia Desktop). On other targets, members without an equivalent native capability throw `NotImplementedException`.

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

The overload taking `CoreWebView2EnvironmentOptions` is **not** implemented, because that options type is itself unimplemented and could not be honored.

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
> Unlike the static members above, these three are provided by the default WebView2 backend only. An app that opts into the other backend with `UNO_WEBVIEW2_BACKEND=microsoft.web.webview2` gets `NotImplementedException` from them.
>
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

Clearing a large profile can take several seconds and the displayed content may reload underneath.

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
    // args.Response = new CoreWebView2WebResourceResponse(...);
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
| **Android** | ⚠️ Partial | ✅ | ⚠️ | ✅ | Header modification requires re-fetching the resource with HttpClient (only safe for GET/HEAD requests). Session cookies are automatically synchronized. POST request bodies cannot be reliably re-fetched and are not reissued by the implementation, so header changes for POST requests are unsupported. |
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

Starting with Uno 7, WebView2 has two separate backends on Windows:

- Microsoft.Web.WebView2
- WebView2Aot

The WebView2Aot backend is required in order to use WebView2 with [Native AOT](xref:Uno.Features.NativeAOT) on Windows.

The WebView2Aot backend is the default when `net10.0-desktop` or later is the target framework.

If you encounter issues with the WebView2 control on Windows when targeting .NET 10 or later, please file an issue. The previous Microsoft.Web.WebView2 backend can be used by setting the `UNO_WEBVIEW2_BACKEND` environment variable to `microsoft.web.webview2`, for example within `Main()`:

```csharp
public partial class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("UNO_WEBVIEW2_BACKEND", "microsoft.web.webview2");
        var host = UnoPlatformHostBuilder.Create()
            // …
            ;
    }
}
```
