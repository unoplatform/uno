---
uid: Uno.Controls.WebView2
---

# `WebView2` (`WebView`)

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

> [!IMPORTANT]
> If your project's desktop builder in `Platforms/Desktop/Program.cs` uses `.UseWindows()`, you'll also need to add the `<UnoUseWebView2WPF>true</UnoUseWebView2WPF>` property for the integration to work. However, it is recommended to [migrate to `.UseWin32()`](xref:Uno.Development.MigratingToUno6) for better performance and reliability.

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

## iOS specifics

From macOS, inspecting applications using `WebView2` controls using the Safari Developer Tools is possible. [Here's](https://developer.apple.com/documentation/safari-developer-tools/inspecting-ios) a detailed guide on how to do it. To make this work, enable this feature in your app by adding the following capabilities in your `App.Xaml.cs`:

```csharp
public App()
{
    this.InitializeComponent();
#if __IOS__
    Uno.UI.FeatureConfiguration.WebView2.IsInspectable = true;
#endif
}
```

> [!IMPORTANT]
>
> This feature will only work for security reasons when the application runs in Debug mode.

## X11 specifics

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

## WinAppSDK Specifics

When using the WebView2 and running on WinAppSDK, make sure to create an `x64` or `ARM64` configuration:

- In the Visual Studio configuration manager, create an `x64` or `ARM64` solution configuration
- Assign it to the Uno Platform project
- Debug your application using the configuration relevant to your current environment

## Tutorials and getting started guides

As the WebView2 control we can use in Uno Apps is based on the same you can use in WinUI3 apps, you can follow the same tutorials and documentation available for WinUI3 to implement WebView2 in your Uno applications, with some adjustments for platform-specific features and capabilities.

A good starting point is the official Microsoft documentation on [WebView2](https://learn.microsoft.com/de-de/microsoft-edge/webview2/platforms/winui3-windows-app-sdk) and its [getting started guide](https://learn.microsoft.com/de-de/microsoft-edge/webview2/get-started/winui#step-6-add-a-webview2-control).
